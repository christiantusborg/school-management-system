using Odin.Api.Base.Letters;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.PartnerCertificates;

/// <summary>
/// Admission-Office management of partner cooperation certificates: one per
/// (partner, school), designed in the shared certificate editor, rendered live
/// on download. The partner drawer's Certificates tab lists only certificates
/// that exist; "+ Add" offers the schools that don't have one yet.
/// </summary>
[Route("/v1/admin/partners/{partnerId:guid}/certificates")]
[EndpointTag("Admin.PartnerCertificates")]
public sealed class AdminV1PartnerCertificatesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/partners/{partnerId:guid}/certificates", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/certificates", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partner-certificates/{certificateId:guid}", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/partner-certificates/{certificateId:guid}", RenameAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/partner-certificates/{certificateId:guid}", SaveLayoutAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/partner-certificates/{certificateId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partner-certificates/{certificateId:guid}/preview", PreviewAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partner-certificates/{certificateId:guid}/download", DownloadAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class CreateBody
    {
        public Guid SchoolId { get; init; }
        public string? Title { get; init; }
    }

    public sealed class RenameBody
    {
        public string? Title { get; init; }
    }

    public sealed class LayoutBody
    {
        public string? CertificateLayoutJson { get; init; }
    }

    private static async Task<IResult> ListAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var partnerExists = await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (!partnerExists) return Results.NotFound();

        var items = await db.PartnerCertificates
            .Where(c => c.PartnerId == partnerId && c.DeletedAt == null)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                partnerCertificateId = c.PartnerCertificateId,
                schoolId = c.SchoolId,
                schoolName = db.Schools.Where(s => s.SchoolId == c.SchoolId).Select(s => s.Name).FirstOrDefault(),
                title = c.Title,
                updatedAt = c.UpdatedAt ?? c.CreatedAt,
            })
            .ToListAsync(ct);

        var usedSchoolIds = items.Select(i => i.schoolId).ToHashSet();
        var schoolsAvailable = (await db.Schools
                .Where(s => s.DeletedAt == null)
                .OrderBy(s => s.Name)
                .Select(s => new { schoolId = s.SchoolId, name = s.Name })
                .ToListAsync(ct))
            .Where(s => !usedSchoolIds.Contains(s.schoolId))
            .ToList();

        return Results.Ok(new { items, schoolsAvailable });
    }

    private static async Task<IResult> CreateAsync(
        Guid partnerId, [FromBody] CreateBody body, OdinDbContext db, CancellationToken ct)
    {
        var partnerExists = await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (!partnerExists) return Results.NotFound();
        var schoolExists = await db.Schools.AnyAsync(s => s.SchoolId == body.SchoolId && s.DeletedAt == null, ct);
        if (!schoolExists) return Results.BadRequest(new { error = "Unknown school." });

        var duplicate = await db.PartnerCertificates.AnyAsync(c =>
            c.PartnerId == partnerId && c.SchoolId == body.SchoolId && c.DeletedAt == null, ct);
        if (duplicate)
            return Results.Conflict(new { error = "This partner already has a certificate for that school." });

        var cert = new PartnerCertificate
        {
            PartnerId = partnerId,
            SchoolId = body.SchoolId,
            Title = string.IsNullOrWhiteSpace(body.Title) ? "Certificate of Partnership" : body.Title.Trim(),
            CertificateLayoutJson = PartnerCertificateService.DefaultLayoutJson(),
        };
        db.PartnerCertificates.Add(cert);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerCertificateId = cert.PartnerCertificateId });
    }

    private static async Task<IResult> GetAsync(Guid certificateId, OdinDbContext db, CancellationToken ct)
    {
        var cert = await db.PartnerCertificates
            .Where(c => c.PartnerCertificateId == certificateId && c.DeletedAt == null)
            .Select(c => new
            {
                partnerCertificateId = c.PartnerCertificateId,
                partnerId = c.PartnerId,
                schoolId = c.SchoolId,
                title = c.Title,
                certificateLayoutJson = c.CertificateLayoutJson,
            })
            .FirstOrDefaultAsync(ct);
        return cert is null ? Results.NotFound() : Results.Ok(cert);
    }

    private static async Task<IResult> RenameAsync(
        Guid certificateId, [FromBody] RenameBody body, OdinDbContext db, CancellationToken ct)
    {
        var cert = await db.PartnerCertificates
            .FirstOrDefaultAsync(c => c.PartnerCertificateId == certificateId && c.DeletedAt == null, ct);
        if (cert is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(body.Title))
            return Results.BadRequest(new { error = "Title cannot be empty." });
        cert.Title = body.Title.Trim();
        cert.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { title = cert.Title });
    }

    private static async Task<IResult> SaveLayoutAsync(
        Guid certificateId, [FromBody] LayoutBody body, OdinDbContext db, CancellationToken ct)
    {
        var cert = await db.PartnerCertificates
            .FirstOrDefaultAsync(c => c.PartnerCertificateId == certificateId && c.DeletedAt == null, ct);
        if (cert is null) return Results.NotFound();
        if (CertificateLayout.TryParse(body.CertificateLayoutJson) is null)
            return Results.BadRequest(new { error = "Invalid layout JSON." });
        cert.CertificateLayoutJson = body.CertificateLayoutJson;
        cert.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> DeleteAsync(Guid certificateId, OdinDbContext db, CancellationToken ct)
    {
        var cert = await db.PartnerCertificates
            .FirstOrDefaultAsync(c => c.PartnerCertificateId == certificateId && c.DeletedAt == null, ct);
        if (cert is null) return Results.NotFound();
        cert.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static async Task<IResult> PreviewAsync(
        Guid certificateId, [FromBody] LayoutBody body,
        PartnerCertificateService service, CancellationToken ct)
    {
        var pdf = await service.RenderAsync(certificateId, ct, layoutJsonOverride: body.CertificateLayoutJson);
        if (pdf is null) return Results.NotFound();
        return Results.File(pdf, "application/pdf", "partner-certificate-preview.pdf");
    }

    private static async Task<IResult> DownloadAsync(
        Guid certificateId, PartnerCertificateService service, CancellationToken ct)
    {
        var pdf = await service.RenderAsync(certificateId, ct);
        if (pdf is null) return Results.NotFound();
        return Results.File(pdf, "application/pdf", $"partner-certificate-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}

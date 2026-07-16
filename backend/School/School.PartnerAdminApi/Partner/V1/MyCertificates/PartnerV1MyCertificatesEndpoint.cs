using Odin.Api.Base.Letters;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyCertificates;

/// <summary>
/// Partner-portal view of the partner's own cooperation certificates: list the
/// ones the Admission Office created and download each as a live-rendered PDF.
/// Read-only for partners.
/// </summary>
[Route("/v1/partner/my/certificates")]
[EndpointTag("Partner.MyCertificates")]
public sealed class PartnerV1MyCertificatesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my/certificates", ListAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/certificates/{certificateId:guid}/download", DownloadAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var items = await db.PartnerCertificates
            .Where(c => c.PartnerId == partnerId && c.DeletedAt == null)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                partnerCertificateId = c.PartnerCertificateId,
                schoolName = db.Schools.Where(s => s.SchoolId == c.SchoolId).Select(s => s.Name).FirstOrDefault(),
                title = c.Title,
                updatedAt = c.UpdatedAt ?? c.CreatedAt,
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> DownloadAsync(
        Guid certificateId, HttpContext httpContext, OdinDbContext db,
        PartnerCertificateService service, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var owned = await db.PartnerCertificates.AnyAsync(c =>
            c.PartnerCertificateId == certificateId && c.PartnerId == partnerId && c.DeletedAt == null, ct);
        if (!owned) return Results.NotFound();

        var pdf = await service.RenderAsync(certificateId, ct);
        if (pdf is null) return Results.NotFound();
        return Results.File(pdf, "application/pdf", $"partner-certificate-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}

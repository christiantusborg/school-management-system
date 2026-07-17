using Odin.Api.Base.Letters;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyCertificates;

/// <summary>
/// Partner-portal view of the partner's own documents (certificates,
/// authorization letters, diplomas, …): list what the Admission Office issued
/// and download each as a live-rendered PDF. Read-only for partners.
/// </summary>
[Route("/v1/partner/my/certificates")]
[EndpointTag("Partner.MyCertificates")]
public sealed class PartnerV1MyCertificatesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my/certificates", ListAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/certificates/{documentId:guid}/download", DownloadAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var items = (await db.PartnerDocuments
            .Where(d => d.PartnerId == partnerId && d.DeletedAt == null)
            .OrderBy(d => d.CreatedAt)
            .Select(d => new
            {
                d.PartnerDocumentId,
                TypeName = db.PartnerDocumentTypes
                    .Where(t => t.PartnerDocumentTypeId == d.PartnerDocumentTypeId)
                    .Select(t => t.Name).FirstOrDefault(),
                UpdatedAt = d.UpdatedAt ?? d.CreatedAt,
            })
            .ToListAsync(ct))
            .Select(d => new
            {
                partnerDocumentId = d.PartnerDocumentId,
                typeName = d.TypeName ?? "Document",
                updatedAt = d.UpdatedAt,
            })
            .ToList();
        return Results.Ok(new { items });
    }

    private static async Task<IResult> DownloadAsync(
        Guid documentId, HttpContext httpContext, OdinDbContext db,
        PartnerDocumentService service, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var owned = await db.PartnerDocuments.AnyAsync(d =>
            d.PartnerDocumentId == documentId && d.PartnerId == partnerId && d.DeletedAt == null, ct);
        if (!owned) return Results.NotFound();

        var pdf = await service.RenderDocumentAsync(documentId, ct);
        if (pdf is null) return Results.NotFound();
        return Results.File(pdf, "application/pdf", $"partner-document-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}

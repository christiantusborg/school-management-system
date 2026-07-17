using Odin.Api.Base.Letters;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.PartnerDocuments;

/// <summary>
/// Documents issued to ONE partner: pick a globally configured type, fill out
/// its fields in a dialog, done. Any number of documents per type. The PDF is
/// rendered live from the type's shared template + this document's values.
/// </summary>
[Route("/v1/admin/partners/{partnerId:guid}/documents")]
[EndpointTag("Admin.PartnerDocuments")]
public sealed class AdminV1PartnerDocumentsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/partners/{partnerId:guid}/documents", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/documents", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/partner-documents/{documentId:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/partner-documents/{documentId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partner-documents/{documentId:guid}/download", DownloadAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class WriteBody
    {
        public Guid? PartnerDocumentTypeId { get; init; }
        public Dictionary<string, string?>? Values { get; init; }
    }

    private static string SerializeValues(Dictionary<string, string?>? values) =>
        System.Text.Json.JsonSerializer.Serialize(
            (values ?? []).Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value!.Trim()));

    private static async Task<IResult> ListAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var partnerExists = await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (!partnerExists) return Results.NotFound();

        var items = (await db.PartnerDocuments
            .Where(d => d.PartnerId == partnerId && d.DeletedAt == null)
            .OrderBy(d => d.CreatedAt)
            .Select(d => new
            {
                d.PartnerDocumentId,
                d.PartnerDocumentTypeId,
                TypeName = db.PartnerDocumentTypes
                    .Where(t => t.PartnerDocumentTypeId == d.PartnerDocumentTypeId)
                    .Select(t => t.Name).FirstOrDefault(),
                d.FieldValuesJson,
                UpdatedAt = d.UpdatedAt ?? d.CreatedAt,
            })
            .ToListAsync(ct))
            .Select(d => new
            {
                partnerDocumentId = d.PartnerDocumentId,
                partnerDocumentTypeId = d.PartnerDocumentTypeId,
                typeName = d.TypeName ?? "(deleted type)",
                values = PartnerDocumentService.ParseValues(d.FieldValuesJson),
                updatedAt = d.UpdatedAt,
            })
            .ToList();

        // Types + their fields drive the fill-out dialog.
        var types = (await db.PartnerDocumentTypes
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.Name)
            .Select(t => new { t.PartnerDocumentTypeId, t.Name, t.FieldsJson })
            .ToListAsync(ct))
            .Select(t => new
            {
                partnerDocumentTypeId = t.PartnerDocumentTypeId,
                name = t.Name,
                fields = PartnerDocumentService.ParseFields(t.FieldsJson)
                    .Select(f => new { id = f.Id, label = f.Label, type = f.Type, source = f.Source })
                    .ToList(),
            })
            .ToList();

        var partnerSources = PartnerDocumentService.PartnerSources
            .Select(s => new { key = s.Key, label = s.Label })
            .ToList();

        return Results.Ok(new { items, types, partnerSources });
    }

    private static async Task<IResult> CreateAsync(
        Guid partnerId, [FromBody] WriteBody body, OdinDbContext db, CancellationToken ct)
    {
        var partnerExists = await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (!partnerExists) return Results.NotFound();
        var typeExists = await db.PartnerDocumentTypes.AnyAsync(t =>
            t.PartnerDocumentTypeId == body.PartnerDocumentTypeId && t.DeletedAt == null, ct);
        if (!typeExists) return Results.BadRequest(new { error = "Unknown document type." });

        var doc = new PartnerDocument
        {
            PartnerId = partnerId,
            PartnerDocumentTypeId = body.PartnerDocumentTypeId!.Value,
            FieldValuesJson = SerializeValues(body.Values),
        };
        db.PartnerDocuments.Add(doc);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerDocumentId = doc.PartnerDocumentId });
    }

    private static async Task<IResult> UpdateAsync(
        Guid documentId, [FromBody] WriteBody body, OdinDbContext db, CancellationToken ct)
    {
        var doc = await db.PartnerDocuments
            .FirstOrDefaultAsync(d => d.PartnerDocumentId == documentId && d.DeletedAt == null, ct);
        if (doc is null) return Results.NotFound();
        doc.FieldValuesJson = SerializeValues(body.Values);
        doc.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerDocumentId = documentId });
    }

    private static async Task<IResult> DeleteAsync(Guid documentId, OdinDbContext db, CancellationToken ct)
    {
        var doc = await db.PartnerDocuments
            .FirstOrDefaultAsync(d => d.PartnerDocumentId == documentId && d.DeletedAt == null, ct);
        if (doc is null) return Results.NotFound();
        doc.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static async Task<IResult> DownloadAsync(
        Guid documentId, PartnerDocumentService service, CancellationToken ct)
    {
        var pdf = await service.RenderDocumentAsync(documentId, ct);
        if (pdf is null) return Results.NotFound();
        return Results.File(pdf, "application/pdf", $"partner-document-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}

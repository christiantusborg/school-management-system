using Odin.Api.Base.Letters;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.PartnerDocuments;

/// <summary>
/// System Config → Partner Documents: globally configured document types.
/// Each type owns ONE template (designed in the certificate editor) and a set
/// of fill-out fields (free text / date / file upload / partner-profile
/// field). Partner documents of a type always render this shared template.
/// </summary>
[Route("/v1/admin/partner-document-types")]
[EndpointTag("Admin.PartnerDocumentTypes")]
public sealed class AdminV1PartnerDocumentTypesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/partner-document-types", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partner-document-types", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partner-document-types/{typeId:guid}", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/partner-document-types/{typeId:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/partner-document-types/{typeId:guid}", SaveLayoutAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/partner-document-types/{typeId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partner-document-types/{typeId:guid}/preview", PreviewAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class FieldDto
    {
        public string? Id { get; init; }
        public string? Label { get; init; }
        /// <summary>"text" | "date" | "image" | "partner".</summary>
        public string? Type { get; init; }
        /// <summary>Partner-profile source key when Type == "partner".</summary>
        public string? Source { get; init; }
    }

    public sealed class WriteBody
    {
        public string? Name { get; init; }
        public List<FieldDto>? Fields { get; init; }
    }

    public sealed class LayoutBody
    {
        public string? CertificateLayoutJson { get; init; }
    }

    private static (List<PartnerDocField> Fields, string? Error) NormalizeFields(List<FieldDto>? input)
    {
        var fields = new List<PartnerDocField>();
        foreach (var f in input ?? [])
        {
            if (string.IsNullOrWhiteSpace(f.Label))
                return (fields, "Every field needs a label.");
            var type = (f.Type ?? PartnerDocumentService.TextField).Trim().ToLowerInvariant();
            if (type is not (PartnerDocumentService.TextField or PartnerDocumentService.DateField
                or PartnerDocumentService.ImageField or PartnerDocumentService.PartnerField))
                return (fields, $"Unknown field type '{f.Type}'.");
            if (type == PartnerDocumentService.PartnerField
                && !PartnerDocumentService.PartnerSources.Any(s => s.Key == f.Source))
                return (fields, $"Field '{f.Label}' needs a valid partner source.");
            fields.Add(new PartnerDocField(
                string.IsNullOrWhiteSpace(f.Id) ? Guid.NewGuid().ToString("N") : f.Id.Trim(),
                f.Label.Trim(),
                type,
                type == PartnerDocumentService.PartnerField ? f.Source : null));
        }
        var duplicateToken = fields.GroupBy(f => f.Token).FirstOrDefault(g => g.Count() > 1);
        if (duplicateToken is not null)
            return (fields, $"Two fields resolve to the same tag {duplicateToken.Key} — labels must differ.");
        return (fields, null);
    }

    private static object FieldOut(PartnerDocField f) =>
        new { id = f.Id, label = f.Label, type = f.Type, source = f.Source, token = f.Token };

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = (await db.PartnerDocumentTypes
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.PartnerDocumentTypeId,
                t.Name,
                t.FieldsJson,
                UpdatedAt = t.UpdatedAt ?? t.CreatedAt,
                InUse = db.PartnerDocuments.Count(d => d.PartnerDocumentTypeId == t.PartnerDocumentTypeId && d.DeletedAt == null),
            })
            .ToListAsync(ct))
            .Select(t => new
            {
                partnerDocumentTypeId = t.PartnerDocumentTypeId,
                name = t.Name,
                fields = PartnerDocumentService.ParseFields(t.FieldsJson).Select(FieldOut).ToList(),
                inUse = t.InUse,
                updatedAt = t.UpdatedAt,
            })
            .ToList();

        var partnerSources = PartnerDocumentService.PartnerSources
            .Select(s => new { key = s.Key, label = s.Label })
            .ToList();

        return Results.Ok(new { items, partnerSources });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] WriteBody body, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return Results.BadRequest(new { error = "Name is required." });
        var (fields, error) = NormalizeFields(body.Fields);
        if (error is not null) return Results.BadRequest(new { error });

        var type = new PartnerDocumentType
        {
            Name = body.Name.Trim(),
            FieldsJson = PartnerDocumentService.SerializeFields(fields),
            LayoutJson = PartnerDocumentService.DefaultLayoutJson(),
        };
        db.PartnerDocumentTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerDocumentTypeId = type.PartnerDocumentTypeId });
    }

    private static async Task<IResult> GetAsync(Guid typeId, OdinDbContext db, CancellationToken ct)
    {
        var type = await db.PartnerDocumentTypes
            .Where(t => t.PartnerDocumentTypeId == typeId && t.DeletedAt == null)
            .Select(t => new { t.PartnerDocumentTypeId, t.Name, t.FieldsJson, t.LayoutJson })
            .FirstOrDefaultAsync(ct);
        if (type is null) return Results.NotFound();

        var fields = PartnerDocumentService.ParseFields(type.FieldsJson);
        // Tag palette for the designer: base partner tags + one per field.
        var tags = new List<object>
        {
            new { token = "[partner name]", source = "partner.name" },
            new { token = "[date]", source = "today" },
            new { token = "[valid until]", source = "partner.contractEnd" },
        };
        tags.AddRange(fields.Select(f => (object)new { token = f.Token, source = $"field.{f.Type}" }));

        return Results.Ok(new
        {
            partnerDocumentTypeId = type.PartnerDocumentTypeId,
            name = type.Name,
            fields = fields.Select(FieldOut).ToList(),
            certificateLayoutJson = type.LayoutJson,
            tags,
        });
    }

    private static async Task<IResult> UpdateAsync(
        Guid typeId, [FromBody] WriteBody body, OdinDbContext db, CancellationToken ct)
    {
        var type = await db.PartnerDocumentTypes
            .FirstOrDefaultAsync(t => t.PartnerDocumentTypeId == typeId && t.DeletedAt == null, ct);
        if (type is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(body.Name))
            return Results.BadRequest(new { error = "Name is required." });
        var (fields, error) = NormalizeFields(body.Fields);
        if (error is not null) return Results.BadRequest(new { error });

        type.Name = body.Name.Trim();
        type.FieldsJson = PartnerDocumentService.SerializeFields(fields);
        type.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerDocumentTypeId = typeId });
    }

    private static async Task<IResult> SaveLayoutAsync(
        Guid typeId, [FromBody] LayoutBody body, OdinDbContext db, CancellationToken ct)
    {
        var type = await db.PartnerDocumentTypes
            .FirstOrDefaultAsync(t => t.PartnerDocumentTypeId == typeId && t.DeletedAt == null, ct);
        if (type is null) return Results.NotFound();
        if (CertificateLayout.TryParse(body.CertificateLayoutJson) is null)
            return Results.BadRequest(new { error = "Invalid layout JSON." });
        type.LayoutJson = body.CertificateLayoutJson;
        type.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> DeleteAsync(Guid typeId, OdinDbContext db, CancellationToken ct)
    {
        var type = await db.PartnerDocumentTypes
            .FirstOrDefaultAsync(t => t.PartnerDocumentTypeId == typeId && t.DeletedAt == null, ct);
        if (type is null) return Results.NotFound();
        var inUse = await db.PartnerDocuments
            .CountAsync(d => d.PartnerDocumentTypeId == typeId && d.DeletedAt == null, ct);
        if (inUse > 0)
            return Results.Conflict(new { error = $"This type is used by {inUse} partner document(s). Remove those first." });
        type.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static async Task<IResult> PreviewAsync(
        Guid typeId, [FromBody] LayoutBody body,
        PartnerDocumentService service, CancellationToken ct)
    {
        var pdf = await service.RenderTypePreviewAsync(typeId, body.CertificateLayoutJson, ct);
        if (pdf is null) return Results.NotFound();
        return Results.File(pdf, "application/pdf", "partner-document-preview.pdf");
    }
}

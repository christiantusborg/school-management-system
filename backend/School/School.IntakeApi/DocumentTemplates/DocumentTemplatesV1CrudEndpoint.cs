using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace School.IntakeApi.DocumentTemplates;

/// <summary>
/// Admin CRUD for intake document templates (strategy + visual mapping),
/// their 1:1 base asset (the PDF to overlay / AcroForm to fill) and the
/// firm-wide image bank (logos, letterheads, stamps). Ported from QuVian
/// core with the same routes and shapes; base64 payloads as in the core
/// client.
/// </summary>
[Route("/v1/intake/document-templates")]
[EndpointTag("Intake.DocumentTemplates")]
public sealed class DocumentTemplatesV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/intake/document-templates", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/document-templates", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/document-templates/{id:guid}", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/intake/document-templates/{id:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/intake/document-templates/{id:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/document-templates/{id:guid}/restore", RestoreAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/document-templates/{id:guid}/asset", UploadAssetAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/document-templates/{id:guid}/asset", GetAssetAsync).RequireAuthorization("AdminOnly");

        app.MapGet("/v1/intake/document-template-images", ImagesListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/document-template-images", ImagesUploadAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/document-template-images/{id:guid}/file", ImagesFileAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/intake/document-template-images/{id:guid}", ImagesDeleteAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    // 20 MB cap on template base files / images: authoring assets, not bulk storage.
    private const int MaxAssetBytes = 20 * 1024 * 1024;

    private static IResult Ok(object data) => Results.Ok(new { success = true, data });
    private static IResult Fail(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { success = false, error }, statusCode: status);
    private static string? Caller(HttpContext http) => http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public sealed class WriteRequest
    {
        public string? Name { get; init; }
        public string? Strategy { get; init; }
        public string? BaseAssetRef { get; init; }
        public string? MappingJson { get; init; }
    }

    public sealed class AssetUploadRequest
    {
        public string? Filename { get; init; }
        public string? ContentType { get; init; }
        public string? BytesBase64 { get; init; }
    }

    public sealed class ImageUploadRequest
    {
        public string? Name { get; init; }
        public string? MimeType { get; init; }
        public string? BytesBase64 { get; init; }
    }

    private static object Dto(DocumentTemplate e, bool hasAsset) => new
    {
        documentTemplateId = e.DocumentTemplateId,
        name = e.Name,
        strategy = e.Strategy.ToString(),
        baseAssetRef = e.BaseAssetRef,
        mappingJson = e.MappingJson,
        hasAsset,
        isFirmLibrary = true,
        groupId = (Guid?)null,
        createdByUserId = e.CreatedByUserId,
        createdAt = e.CreatedAt,
        modifiedAt = e.ModifiedAt,
        deletedAt = e.DeletedAt,
    };

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.IntakeDocumentTemplates
            .Where(e => includeDeleted || e.DeletedAt == null)
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                Entity = e,
                HasAsset = db.IntakeDocumentTemplateAssets.Any(a => a.DocumentTemplateId == e.DocumentTemplateId),
            })
            .ToListAsync(ct);
        return Ok(new { items = items.Select(x => Dto(x.Entity, x.HasAsset)) });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] WriteRequest body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (!Enum.TryParse<DocumentStrategy>(body.Strategy, ignoreCase: true, out var strategy))
            return Fail("strategy_invalid");
        var e = new DocumentTemplate
        {
            Name = body.Name.Trim(),
            Strategy = strategy,
            BaseAssetRef = body.BaseAssetRef,
            MappingJson = string.IsNullOrWhiteSpace(body.MappingJson) ? "{}" : body.MappingJson,
            CreatedByUserId = Caller(http),
        };
        db.IntakeDocumentTemplates.Add(e);
        await db.SaveChangesAsync(ct);
        return Ok(Dto(e, hasAsset: false));
    }

    private static async Task<IResult> GetAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeDocumentTemplates.FirstOrDefaultAsync(x => x.DocumentTemplateId == id, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        var hasAsset = await db.IntakeDocumentTemplateAssets.AnyAsync(a => a.DocumentTemplateId == id, ct);
        return Ok(Dto(e, hasAsset));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, [FromBody] WriteRequest body, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeDocumentTemplates.FirstOrDefaultAsync(x => x.DocumentTemplateId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (!Enum.TryParse<DocumentStrategy>(body.Strategy, ignoreCase: true, out var strategy))
            return Fail("strategy_invalid");
        e.Name = body.Name.Trim();
        e.Strategy = strategy;
        e.BaseAssetRef = body.BaseAssetRef;
        if (!string.IsNullOrWhiteSpace(body.MappingJson)) e.MappingJson = body.MappingJson;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var hasAsset = await db.IntakeDocumentTemplateAssets.AnyAsync(a => a.DocumentTemplateId == id, ct);
        return Ok(Dto(e, hasAsset));
    }

    private static async Task<IResult> DeleteAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeDocumentTemplates.FirstOrDefaultAsync(x => x.DocumentTemplateId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        e.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }

    private static async Task<IResult> RestoreAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeDocumentTemplates.FirstOrDefaultAsync(x => x.DocumentTemplateId == id && x.DeletedAt != null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        e.DeletedAt = null;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var hasAsset = await db.IntakeDocumentTemplateAssets.AnyAsync(a => a.DocumentTemplateId == id, ct);
        return Ok(Dto(e, hasAsset));
    }

    // ── 1:1 base asset ────────────────────────────────────────────────────

    private static async Task<IResult> UploadAssetAsync(
        Guid id, [FromBody] AssetUploadRequest body, OdinDbContext db, CancellationToken ct)
    {
        var template = await db.IntakeDocumentTemplates
            .FirstOrDefaultAsync(x => x.DocumentTemplateId == id && x.DeletedAt == null, ct);
        if (template is null) return Fail("not_found", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(body.Filename) || string.IsNullOrWhiteSpace(body.BytesBase64))
            return Fail("file_required");

        byte[] bytes;
        try { bytes = Convert.FromBase64String(body.BytesBase64); }
        catch { return Fail("bytes_not_base64"); }
        if (bytes.Length == 0 || bytes.Length > MaxAssetBytes) return Fail("file_too_large");

        var asset = await db.IntakeDocumentTemplateAssets.FirstOrDefaultAsync(a => a.DocumentTemplateId == id, ct);
        if (asset is null)
        {
            asset = new DocumentTemplateAsset { DocumentTemplateId = id };
            db.IntakeDocumentTemplateAssets.Add(asset);
        }
        asset.Filename = body.Filename.Trim();
        asset.ContentType = string.IsNullOrWhiteSpace(body.ContentType) ? "application/octet-stream" : body.ContentType;
        asset.Bytes = bytes;
        asset.SizeBytes = bytes.Length;
        asset.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            documentTemplateAssetId = asset.DocumentTemplateAssetId,
            documentTemplateId = id,
            filename = asset.Filename,
            contentType = asset.ContentType,
            sizeBytes = asset.SizeBytes,
            modifiedAt = asset.ModifiedAt,
        });
    }

    private static async Task<IResult> GetAssetAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var asset = await db.IntakeDocumentTemplateAssets.FirstOrDefaultAsync(a => a.DocumentTemplateId == id, ct);
        if (asset is null) return Fail("not_found", StatusCodes.Status404NotFound);
        return Ok(new
        {
            documentTemplateAssetId = asset.DocumentTemplateAssetId,
            documentTemplateId = asset.DocumentTemplateId,
            filename = asset.Filename,
            contentType = asset.ContentType,
            bytesBase64 = Convert.ToBase64String(asset.Bytes),
            sizeBytes = asset.SizeBytes,
            modifiedAt = asset.ModifiedAt,
        });
    }

    // ── Image bank ────────────────────────────────────────────────────────

    private static async Task<IResult> ImagesListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.IntakeDocumentTemplateImages
            .Where(e => includeDeleted || e.DeletedAt == null)
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                documentTemplateImageId = e.DocumentTemplateImageId,
                name = e.Name,
                mimeType = e.MimeType,
                sizeBytes = e.SizeBytes,
                uploadedAt = e.UploadedAt,
                deletedAt = e.DeletedAt,
            })
            .ToListAsync(ct);
        return Ok(new { items, total = items.Count });
    }

    private static async Task<IResult> ImagesUploadAsync(
        [FromBody] ImageUploadRequest body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.BytesBase64))
            return Fail("file_required");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(body.BytesBase64); }
        catch { return Fail("bytes_not_base64"); }
        if (bytes.Length == 0 || bytes.Length > MaxAssetBytes) return Fail("file_too_large");

        var e = new DocumentTemplateImage
        {
            Name = body.Name.Trim(),
            MimeType = string.IsNullOrWhiteSpace(body.MimeType) ? "image/png" : body.MimeType,
            DataBase64 = body.BytesBase64,
            SizeBytes = bytes.Length,
            UploadedByUserId = Caller(http),
        };
        db.IntakeDocumentTemplateImages.Add(e);
        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            documentTemplateImageId = e.DocumentTemplateImageId,
            name = e.Name,
            mimeType = e.MimeType,
            sizeBytes = e.SizeBytes,
            uploadedAt = e.UploadedAt,
        });
    }

    private static async Task<IResult> ImagesFileAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeDocumentTemplateImages
            .FirstOrDefaultAsync(x => x.DocumentTemplateImageId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        return Ok(new
        {
            documentTemplateImageId = e.DocumentTemplateImageId,
            name = e.Name,
            mimeType = e.MimeType,
            bytesBase64 = e.DataBase64,
        });
    }

    private static async Task<IResult> ImagesDeleteAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeDocumentTemplateImages
            .FirstOrDefaultAsync(x => x.DocumentTemplateImageId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        e.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { documentTemplateImageId = e.DocumentTemplateImageId, deletedAt = e.DeletedAt });
    }
}

using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.ProgrammeApi;

[Route("/v1/admin/letter-assets")]
[EndpointTag("Admin.LetterAssets")]
public sealed class AdminLetterAssetsV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/letter-assets", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/letter-assets", UploadAsync)
            .RequireAuthorization("AdminOnly").DisableAntiforgery();
        app.MapGet("/v1/admin/letter-assets/{id:guid}/file", GetFileAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/letter-assets/{id:guid}", SoftDeleteAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.LetterAssets
            .Where(a => a.DeletedAt == null)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new
            {
                letterAssetId = a.LetterAssetId,
                name = a.Name,
                mimeType = a.MimeType,
                sizeBytes = a.SizeBytes,
                uploadedAt = a.UploadedAt,
            })
            .ToListAsync(ct);
        return Results.Ok(new { items, total = items.Count });
    }

    private static async Task<IResult> UploadAsync(
        OdinDbContext db, IFileStorage storage, IFormFile file, [FromForm] string? name, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "file is required" });
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { message = "only image uploads are accepted" });

        var assetId = Guid.NewGuid();
        var safeName = string.IsNullOrWhiteSpace(name)
            ? Path.GetFileNameWithoutExtension(file.FileName)
            : name.Trim();
        var fileName = file.FileName;

        string storagePath;
        await using (var stream = file.OpenReadStream())
        {
            storagePath = await storage.SaveAsync(
                stream,
                $"letter-assets/{assetId}-{fileName}",
                ct);
        }

        var asset = new LetterAsset
        {
            LetterAssetId = assetId,
            Name = safeName,
            MimeType = file.ContentType,
            StoragePath = storagePath,
            SizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
        };
        db.LetterAssets.Add(asset);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/v1/admin/letter-assets/{asset.LetterAssetId}", new
        {
            letterAssetId = asset.LetterAssetId,
            name = asset.Name,
            mimeType = asset.MimeType,
            sizeBytes = asset.SizeBytes,
            uploadedAt = asset.UploadedAt,
        });
    }

    private static async Task<IResult> GetFileAsync(
        OdinDbContext db, IFileStorage storage, Guid id, CancellationToken ct)
    {
        var asset = await db.LetterAssets.FirstOrDefaultAsync(a => a.LetterAssetId == id && a.DeletedAt == null, ct);
        if (asset is null) return Results.NotFound();
        var stream = await storage.OpenReadAsync(asset.StoragePath, ct);
        return Results.File(stream, asset.MimeType);
    }

    private static async Task<IResult> SoftDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var asset = await db.LetterAssets.FirstOrDefaultAsync(a => a.LetterAssetId == id && a.DeletedAt == null, ct);
        if (asset is null) return Results.NotFound();
        asset.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { letterAssetId = id });
    }
}

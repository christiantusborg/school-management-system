using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Odin.Api.Base.Storage;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Streams the actual uploaded file for a partner-owned student's document.
/// Used by the review wizard's preview pane (replaces the previous
/// hard-coded sample-image/sample-pdf in <c>DocPreview.vue</c>).
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/documents/{documentId:guid}/file")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsDocumentFileEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students/{studentId:guid}/documents/{documentId:guid}/file", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid documentId,
        HttpContext httpContext, OdinDbContext db, IFileStorage storage,
        IConfiguration config, IHostEnvironment env, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var ownsStudent = await db.Students
            .AnyAsync(s => s.StudentId == studentId && s.PartnerId == partnerId && s.DeletedAt == null, ct);
        if (!ownsStudent) return Results.NotFound();

        var doc = await db.StudentDocuments
            .Where(d => d.StudentDocumentId == documentId && d.StudentId == studentId && d.DeletedAt == null)
            .Select(d => new { d.StoragePath, d.FileName, d.MimeType, d.StudentId })
            .FirstOrDefaultAsync(ct);
        if (doc is null) return Results.NotFound();

        // Resolve the storage key. New uploads carry it; legacy rows fall
        // back to a directory scan keyed on (studentId, documentId, fileName).
        var storageKey = doc.StoragePath
            ?? FindLegacyKey(config, env, doc.StudentId, documentId, doc.FileName);
        if (storageKey is null) return Results.NotFound();

        Stream stream;
        try { stream = await storage.OpenReadAsync(storageKey, ct); }
        catch (FileNotFoundException) { return Results.NotFound(); }
        catch (DirectoryNotFoundException) { return Results.NotFound(); }

        return Results.File(stream, doc.MimeType ?? "application/octet-stream", doc.FileName);
    }

    private static string? FindLegacyKey(
        IConfiguration config, IHostEnvironment env,
        Guid studentId, Guid documentId, string fileName)
    {
        var configured = config["Storage:LocalRoot"];
        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "uploads")
            : Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured);

        var dir = Path.Combine(root, studentId.ToString());
        if (!Directory.Exists(dir)) return null;

        // First: try the deterministic pattern (new uploads use the docId as
        // the GUID prefix). Falls through to a name-suffix scan for older rows
        // where the prefix is a different random GUID.
        var deterministic = $"{documentId:N}-{fileName}";
        var deterministicPath = Path.Combine(dir, deterministic);
        if (File.Exists(deterministicPath))
            return $"{studentId}/{deterministic}";

        var matches = Directory.EnumerateFiles(dir, $"*-{fileName}").Take(2).ToList();
        if (matches.Count == 1)
            return $"{studentId}/{Path.GetFileName(matches[0])}";

        return null;
    }
}

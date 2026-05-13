using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Odin.Api.Base.Storage;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Streams the actual uploaded file for a student's document so the admin
/// review wizard's preview pane can render it. Mirrors the partner version
/// (PartnerV1MyStudentsDocumentFileEndpoint) but admin-scoped — no partner
/// filter on the student/document.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/documents/{documentId:guid}/file")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsDocumentFileEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/{studentId:guid}/documents/{documentId:guid}/file", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid documentId,
        OdinDbContext db, IFileStorage storage,
        IConfiguration config, IHostEnvironment env, CancellationToken ct)
    {
        var doc = await db.StudentDocuments
            .Where(d => d.StudentDocumentId == documentId && d.StudentId == studentId && d.DeletedAt == null)
            .Select(d => new { d.StoragePath, d.FileName, d.MimeType, d.StudentId })
            .FirstOrDefaultAsync(ct);
        if (doc is null) return Results.NotFound();

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

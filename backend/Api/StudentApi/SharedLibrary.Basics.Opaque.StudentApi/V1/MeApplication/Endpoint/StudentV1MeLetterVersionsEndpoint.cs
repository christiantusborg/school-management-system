using Odin.Api.Base.Storage;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Version history of the calling student's own released letters (dynamic
/// letter types record a version per render). Only letters whose type is
/// student-visible are browsable; the latest PDF stays on the normal
/// document download route.
/// </summary>
[Route("/v1/student/me/letters/{documentId:guid}/versions")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeLetterVersionsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/letters/{documentId:guid}/versions", ListAsync)
            .RequireAuthorization("StudentOnly");
        app.MapGet("/v1/student/me/letters/{documentId:guid}/versions/{versionId:guid}/file", FileAsync)
            .RequireAuthorization("StudentOnly");
        return app;
    }

    private static async Task<bool> OwnsVisibleAsync(
        HttpContext http, OdinDbContext db, Guid documentId, CancellationToken ct)
    {
        var callerId = http.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return false;
        var studentId = await db.Students
            .Where(s => s.UserId == callerId && s.DeletedAt == null)
            .Select(s => (Guid?)s.StudentId)
            .FirstOrDefaultAsync(ct);
        if (studentId is null) return false;
        var docTypeId = await db.StudentDocuments
            .Where(d => d.StudentDocumentId == documentId && d.StudentId == studentId && d.DeletedAt == null)
            .Select(d => (Guid?)d.DocumentTypeId)
            .FirstOrDefaultAsync(ct);
        if (docTypeId is null) return false;
        var hidden = await db.LetterTypeDefinitions.AnyAsync(d =>
            d.DocumentTypeId == docTypeId && d.DeletedAt == null && !d.VisibleToStudent, ct);
        return !hidden;
    }

    private static async Task<IResult> ListAsync(
        Guid documentId, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (!await OwnsVisibleAsync(http, db, documentId, ct)) return Results.NotFound();
        var items = await db.StudentDocumentVersions
            .Where(v => v.StudentDocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new
            {
                studentDocumentVersionId = v.StudentDocumentVersionId,
                versionNumber = v.VersionNumber,
                fileName = v.FileName,
                language = v.Language,
                createdAt = v.CreatedAt,
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> FileAsync(
        Guid documentId, Guid versionId, HttpContext http,
        OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        if (!await OwnsVisibleAsync(http, db, documentId, ct)) return Results.NotFound();
        var v = await db.StudentDocumentVersions
            .Where(x => x.StudentDocumentVersionId == versionId && x.StudentDocumentId == documentId)
            .Select(x => new { x.StoragePath, x.FileName })
            .FirstOrDefaultAsync(ct);
        if (v is null) return Results.NotFound();
        var stream = await storage.OpenReadAsync(v.StoragePath, ct);
        return Results.File(stream, "application/pdf", v.FileName);
    }
}

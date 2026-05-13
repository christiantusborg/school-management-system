namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Streams the actual file bytes for a document owned by the calling student.
/// Mirrors the partner / admin variants but scoped via the caller's UserId
/// → StudentId chain. Used by the student portal's letter download buttons
/// (Offer / Admission / Transcript / Certificate / ProvisionalCertificate)
/// and by the docs review pane to fetch their own uploads.
/// </summary>
[Route("/v1/student/me/documents/{studentDocumentId:guid}/file")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeDocumentFileEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/documents/{studentDocumentId:guid}/file", HandleAsync)
            .RequireAuthorization("StudentOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentDocumentId,
        HttpContext httpContext,
        OdinDbContext db,
        IFileStorage storage,
        CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var student = await db.Students
            .Where(s => s.UserId == callerId && s.DeletedAt == null)
            .Select(s => new { s.StudentId })
            .FirstOrDefaultAsync(ct);
        if (student is null) return Results.NotFound();

        var doc = await db.StudentDocuments
            .Where(d => d.StudentDocumentId == studentDocumentId
                && d.StudentId == student.StudentId
                && d.DeletedAt == null)
            .Select(d => new { d.StoragePath, d.FileName, d.MimeType })
            .FirstOrDefaultAsync(ct);
        if (doc is null) return Results.NotFound();
        if (string.IsNullOrEmpty(doc.StoragePath)) return Results.NotFound();

        Stream stream;
        try { stream = await storage.OpenReadAsync(doc.StoragePath, ct); }
        catch (FileNotFoundException) { return Results.NotFound(); }
        catch (DirectoryNotFoundException) { return Results.NotFound(); }

        return Results.File(stream, doc.MimeType ?? "application/octet-stream", doc.FileName);
    }
}

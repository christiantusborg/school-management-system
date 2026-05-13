using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Student replaces (or first-time uploads) a document of a given type.
///
/// Soft-deletes any existing non-deleted <see cref="StudentDocument"/> the
/// caller has for the same <c>DocumentTypeId</c>, persists the new file
/// via <see cref="IFileStorage"/>, inserts a fresh row with
/// <c>CurrentStatusId = Submitted</c>, and appends a "Replaced by student"
/// audit note. Does not change <c>Enrollment.StatusId</c> — that flip
/// happens on the explicit Resubmit endpoint.
/// </summary>
[Route("/v1/student/me/documents")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeDocumentUploadEndpoint : IEndpointMarker
{
    private const long MaxBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedMimes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png",
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/student/me/documents", HandleAsync)
            .RequireAuthorization("StudentOnly")
            .DisableAntiforgery();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.UserId == callerId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        if (!httpContext.Request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart/form-data required" });

        var form = await httpContext.Request.ReadFormAsync(ct);
        if (!Guid.TryParse(form["documentTypeId"].ToString(), out var documentTypeGuid))
            return Results.BadRequest(new { error = "documentTypeId is required" });
        if (!Guid.TryParse(form["enrollmentId"].ToString(), out var enrollmentGuid))
            return Results.BadRequest(new { error = "enrollmentId is required" });

        // Confirm the enrolment belongs to this student.
        var enrolmentOwned = await db.Enrollments
            .AnyAsync(e => e.StudentEnrollmentId == enrollmentGuid
                && e.StudentId == student.StudentId
                && e.DeletedAt == null, ct);
        if (!enrolmentOwned) return Results.NotFound();

        var file = form.Files["file"];
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "file is required" });
        if (file.Length > MaxBytes)
            return Results.BadRequest(new { error = $"file too large (max {MaxBytes / (1024 * 1024)} MB)" });
        if (!string.IsNullOrEmpty(file.ContentType) && !AllowedMimes.Contains(file.ContentType))
            return Results.BadRequest(new { error = "only PDF, JPG, or PNG files are accepted" });

        var now = DateTime.UtcNow;

        // Soft-delete only the prior row for THIS enrolment + slot. Other
        // enrolments keep their independent rows untouched.
        var prior = await db.StudentDocuments
            .Where(d => d.EnrollmentId == enrollmentGuid
                && d.DocumentTypeId == documentTypeGuid
                && d.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var p in prior) p.DeletedAt = now;

        var safeName = Path.GetFileName(file.FileName ?? "upload");
        var docId = Guid.NewGuid();
        var rel = $"{student.StudentId}/{docId:N}-{safeName}";
        await using (var src = file.OpenReadStream())
            await storage.SaveAsync(src, rel, ct);

        var doc = new StudentDocument
        {
            StudentDocumentId = docId,
            StudentId = student.StudentId,
            EnrollmentId = enrollmentGuid,
            DocumentTypeId = documentTypeGuid,
            FileName = safeName,
            MimeType = file.ContentType ?? "application/octet-stream",
            UploadedAt = now,
            StoragePath = rel,
            CurrentStatusId = DocumentStatusIds.Submitted,
        };
        db.StudentDocuments.Add(doc);
        db.StudentDocumentNotes.Add(new StudentDocumentNote
        {
            StudentDocumentNoteId = Guid.NewGuid(),
            StudentDocumentId = doc.StudentDocumentId,
            StatusId = DocumentStatusIds.Submitted,
            ByUserId = callerId,
            Note = prior.Count > 0 ? "Replaced by student." : "Uploaded by student.",
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            studentDocumentId = doc.StudentDocumentId,
            enrollmentId = enrollmentGuid,
            documentTypeId = documentTypeGuid,
            fileName = doc.FileName,
            uploadedAt = doc.UploadedAt,
        });
    }
}

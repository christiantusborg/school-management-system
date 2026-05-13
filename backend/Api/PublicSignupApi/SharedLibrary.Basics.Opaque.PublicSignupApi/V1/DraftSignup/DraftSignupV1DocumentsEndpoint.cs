using Odin.Api.Base.Authentication;
using Odin.Api.Base.Storage;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Step 5 — uploads. Stores the file via `IFileStorage` under
/// `{studentId}/{guid}-{filename}` and inserts a `StudentDocument` row.
/// DELETE soft-deletes the row (and the underlying blob).
/// </summary>
[Route("/v1/public/draft-signup/documents")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1DocumentsEndpoint : IEndpointMarker
{
    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/public/draft-signup/documents", UploadAsync)
            .AllowAnonymous()
            .DisableAntiforgery();
        app.MapDelete("/v1/public/draft-signup/documents/{id:guid}", DeleteAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> UploadAsync(
        HttpContext http,
        OdinDbContext db,
        WizardSessionService wizard,
        IFileStorage storage,
        CancellationToken ct)
    {
        var session = await WizardTokenAuth.ResolveAsync(http, wizard);
        if (session is null) return WizardTokenAuth.Unauthorised();

        if (!http.Request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart/form-data required" });

        var form = await http.Request.ReadFormAsync(ct);
        if (!Guid.TryParse(form["documentTypeId"].ToString(), out var documentTypeGuid))
            return Results.BadRequest(new { error = "documentTypeId is required" });
        if (!Guid.TryParse(form["specializationId"].ToString(), out var specializationGuid))
            return Results.BadRequest(new { error = "specializationId is required" });
        var file = form.Files["file"];
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "file is required" });
        if (file.Length > MaxBytes)
            return Results.BadRequest(new { error = $"file too large (max {MaxBytes / (1024 * 1024)} MB)" });

        var student = await db.Students.FirstOrDefaultAsync(s => s.StudentId == session.StudentId, ct);
        if (student is null) return Results.BadRequest(new { error = "student record not found" });
        if (student.WizardStep < 5) student.WizardStep = 5;

        // Soft-delete any prior wizard-pending row for the same
        // (specialization, doc-type) so re-uploads don't accumulate.
        var prior = await db.StudentDocuments
            .Where(d => d.StudentId == session.StudentId
                && d.SignupSpecializationId == specializationGuid
                && d.DocumentTypeId == documentTypeGuid
                && d.EnrollmentId == null
                && d.DeletedAt == null)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var p in prior) p.DeletedAt = now;

        var safeName = Path.GetFileName(file.FileName ?? "upload");
        var docId = Guid.NewGuid();
        // Use the doc id as the storage prefix so the path is reconstructible
        // from (StudentId, StudentDocumentId, FileName) without an extra column.
        var rel = $"{session.StudentId}/{docId:N}-{safeName}";
        await using (var src = file.OpenReadStream())
            await storage.SaveAsync(src, rel, ct);

        var doc = new SharedLibrary.Basics.Opaque.Domains.StudentDocument
        {
            StudentDocumentId = docId,
            StudentId = session.StudentId,
            // EnrollmentId stays null until /submit materialises enrolments
            // and attributes wizard uploads via SignupSpecializationId.
            EnrollmentId = null,
            SignupSpecializationId = specializationGuid,
            DocumentTypeId = documentTypeGuid,
            FileName = safeName,
            MimeType = file.ContentType ?? "application/octet-stream",
            UploadedAt = now,
            StoragePath = rel,
            CurrentStatusId = SharedLibrary.Basics.Opaque.Domains.DocumentStatusIds.Submitted,
        };
        db.StudentDocuments.Add(doc);
        db.StudentDocumentNotes.Add(new SharedLibrary.Basics.Opaque.Domains.StudentDocumentNote
        {
            StudentDocumentNoteId = Guid.NewGuid(),
            StudentDocumentId = doc.StudentDocumentId,
            StatusId = SharedLibrary.Basics.Opaque.Domains.DocumentStatusIds.Submitted,
            ByUserId = student.UserId,
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            studentDocumentId = doc.StudentDocumentId,
            specializationId = specializationGuid,
            documentTypeId = documentTypeGuid,
            fileName = doc.FileName,
            uploadedAt = doc.UploadedAt,
        });
    }

    private static async Task<IResult> DeleteAsync(
        HttpContext http,
        Guid id,
        OdinDbContext db,
        WizardSessionService wizard,
        CancellationToken ct)
    {
        var session = await WizardTokenAuth.ResolveAsync(http, wizard);
        if (session is null) return WizardTokenAuth.Unauthorised();

        var doc = await db.StudentDocuments
            .FirstOrDefaultAsync(d => d.StudentDocumentId == id && d.StudentId == session.StudentId && d.DeletedAt == null, ct);
        if (doc is null) return Results.NotFound();

        doc.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { studentDocumentId = id });
    }
}

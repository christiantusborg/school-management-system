using Odin.Api.Base.Documents;
using Odin.Api.Base.Storage;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner uploads a document on behalf of a student in one of their own
/// enrolments. Behaves like the admin endpoint but additionally checks
/// that the partner actually owns the student (via Student.PartnerId) —
/// returns 404 otherwise.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/documents")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsDocumentUploadEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/documents", HandleAsync)
            .RequireAuthorization("PartnerOnly")
            .DisableAntiforgery();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        HttpContext httpContext, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var (callerId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        if (!httpContext.Request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart/form-data required" });

        var form = await httpContext.Request.ReadFormAsync(ct);
        if (!Guid.TryParse(form["documentTypeId"].ToString(), out var documentTypeGuid))
            return Results.BadRequest(new { error = "documentTypeId is required" });
        var isAdditional = bool.TryParse(form["isAdditional"].ToString(), out var ia) && ia;

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.PartnerId == partnerId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        var enrolmentOwned = await db.Enrollments
            .AnyAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.PartnerId == partnerId
                && e.DeletedAt == null, ct);
        if (!enrolmentOwned) return Results.NotFound();

        var file = form.Files["file"];
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "file is required" });
        if (file.Length > DocumentUploadPolicy.MaxBytes)
            return Results.BadRequest(new { error = $"file too large (max {DocumentUploadPolicy.MaxBytes / (1024 * 1024)} MB)" });
        if (!DocumentUploadPolicy.IsAllowed(file.ContentType))
            return Results.BadRequest(new { error = $"only {DocumentUploadPolicy.AllowedHumanReadable} files are accepted" });

        var now = DateTime.UtcNow;

        var existingApproved = await db.StudentDocuments
            .AnyAsync(d => d.EnrollmentId == enrollmentId
                && d.DocumentTypeId == documentTypeGuid
                && d.DeletedAt == null
                && (d.CurrentStatusId == DocumentStatusIds.VerifiedByPartner
                 || d.CurrentStatusId == DocumentStatusIds.VerifiedByEnrolment), ct);
        if (existingApproved && !isAdditional)
        {
            return Results.BadRequest(new
            {
                error = "This document is already approved. Upload it as an additional document instead.",
            });
        }

        var prior = new List<StudentDocument>();
        if (!isAdditional)
        {
            prior = await db.StudentDocuments
                .Where(d => d.EnrollmentId == enrollmentId
                    && d.DocumentTypeId == documentTypeGuid
                    && d.DeletedAt == null)
                .ToListAsync(ct);
            foreach (var p in prior) p.DeletedAt = now;
        }

        var safeName = Path.GetFileName(file.FileName ?? "upload");
        var docId = Guid.NewGuid();
        var rel = $"{studentId}/{docId:N}-{safeName}";
        await using (var src = file.OpenReadStream())
            await storage.SaveAsync(src, rel, ct);

        var doc = new StudentDocument
        {
            StudentDocumentId = docId,
            StudentId = studentId,
            EnrollmentId = enrollmentId,
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
            ByUserId = callerId!,
            Note = isAdditional
                ? "Partner added an additional document."
                : prior.Count > 0 ? "Replaced by partner." : "Uploaded by partner.",
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            studentDocumentId = doc.StudentDocumentId,
            enrollmentId,
            documentTypeId = documentTypeGuid,
            fileName = doc.FileName,
            uploadedAt = doc.UploadedAt,
            isAdditional,
        });
    }
}

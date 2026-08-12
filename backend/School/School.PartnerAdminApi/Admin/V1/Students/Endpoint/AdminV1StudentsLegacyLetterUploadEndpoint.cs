using Odin.Api.Base.Authorization;
using Odin.Api.Base.Storage;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Upload an OLD (pre-Hub) transcript / certificate PDF as the released
/// letter document of an enrolment — for students who graduated before the
/// Hub existed, where no template-generated letter can reproduce the
/// original. The upload takes the released-document slot for that letter
/// type, so Download serves it; Generate/Regenerate afterwards REPLACES it
/// with a generated PDF — the two modes simply overwrite each other, a
/// letter is never both.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{letterKey}/upload")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsLegacyLetterUploadEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{letterKey}/upload", HandleAsync)
            .RequireAuthorization("AdminOnly")
            .DisableAntiforgery();
        return app;
    }

    // Only the graduation documents make sense as legacy uploads.
    private static readonly Dictionary<string, Guid> UploadableTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["transcript"] = SystemDocumentTypeIds.Transcript,
        ["printableTranscript"] = SystemDocumentTypeIds.PrintableTranscript,
        ["certificate"] = SystemDocumentTypeIds.Certificate,
        ["provisionalCertificate"] = SystemDocumentTypeIds.ProvisionalCertificate,
    };

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, string letterKey,
        IFormFile file, HttpContext httpContext, IPermissionService perms,
        OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "student.letters.upload", ct) != AccessLevel.Edit) return Results.Forbid();

        if (!UploadableTypes.TryGetValue(letterKey, out var documentTypeId))
            return Results.BadRequest(new { error = "This letter type does not accept legacy uploads." });
        if (file.Length == 0)
            return Results.BadRequest(new { error = "Empty file." });
        var isPdf = string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
            return Results.BadRequest(new { error = "Only PDF files can be uploaded here." });

        var enrollment = await db.Enrollments.FirstOrDefaultAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (enrollment is null) return Results.NotFound();

        var fileName = Path.GetFileName(file.FileName);
        string storagePath;
        await using (var stream = file.OpenReadStream())
        {
            storagePath = await storage.SaveAsync(
                stream, $"letters/{studentId}/{enrollmentId}/{Guid.NewGuid()}-legacy-{fileName}", ct);
        }

        // Same slot the letter release uses: one active document per
        // (enrollment, document type). Uploading replaces a generated PDF;
        // a later Generate replaces the upload again.
        var existing = await db.StudentDocuments.FirstOrDefaultAsync(d =>
            d.EnrollmentId == enrollmentId && d.DocumentTypeId == documentTypeId && d.DeletedAt == null, ct);
        var now = DateTime.UtcNow;
        if (existing is not null)
        {
            existing.FileName = fileName;
            existing.MimeType = "application/pdf";
            existing.UploadedAt = now;
            existing.StoragePath = storagePath;
            existing.CurrentStatusId = DocumentStatusIds.VerifiedByEnrolment;
        }
        else
        {
            existing = new StudentDocument
            {
                StudentDocumentId = Guid.NewGuid(),
                StudentId = studentId,
                EnrollmentId = enrollmentId,
                DocumentTypeId = documentTypeId,
                FileName = fileName,
                MimeType = "application/pdf",
                UploadedAt = now,
                StoragePath = storagePath,
                CurrentStatusId = DocumentStatusIds.VerifiedByEnrolment,
            };
            db.StudentDocuments.Add(existing);
        }
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { studentDocumentId = existing.StudentDocumentId, fileName, uploadedAt = now });
    }
}

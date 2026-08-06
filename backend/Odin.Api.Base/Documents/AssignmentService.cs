using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains.Assignments;

namespace Odin.Api.Base.Documents;

/// <summary>
/// Shared logic for uploaded assignments (module work + comment chat), used
/// by the admin, partner and student endpoints. Callers are responsible for
/// their own ownership checks (partner-owns-student, student-owns-enrolment);
/// this service only enforces data integrity (subject belongs to the
/// enrolment's current specialization, upload policy, non-empty title).
/// </summary>
public sealed class AssignmentService(OdinDbContext db, IFileStorage storage)
{
    /// <summary>
    /// The full assignments tree for one enrolment: every module of the
    /// enrolment's CURRENT specialization (so the fold-out is complete even
    /// with zero uploads), each with its uploads and their comment threads.
    /// </summary>
    public async Task<object> ListAsync(Guid enrollmentId, CancellationToken ct)
    {
        var specId = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => e.SpecializationId)
            .FirstOrDefaultAsync(ct);

        // Uploaded assignments are organised by the student's MODULE COHORTS;
        // when no cohorts are assigned yet, fall back to the specialization's
        // module list so the panel never goes empty.
        var cohortModules = await (
            from mcs in db.ModuleCohortStudents
            join mc in db.ModuleCohorts on mcs.ModuleCohortId equals mc.ModuleCohortId
            join s in db.Subjects on mc.SubjectId equals s.SubjectId
            where mcs.StudentEnrollmentId == enrollmentId && mcs.DeletedAt == null
                && mc.DeletedAt == null && s.DeletedAt == null
            orderby s.Code
            select new
            {
                s.SubjectId, s.Code, s.Name, mc.CohortNumber,
                CohortTypeName = db.CohortTypes
                    .Where(t => t.CohortTypeId == mc.CohortTypeId)
                    .Select(t => t.Name)
                    .FirstOrDefault() ?? string.Empty,
            })
            .ToListAsync(ct);

        var subjects = cohortModules.Count > 0
            ? cohortModules.Select(x => new
            {
                x.SubjectId, x.Code, x.Name, CohortNumber = (string?)x.CohortNumber,
                IsFinalProject = IsFinalProjectCohortType(x.CohortTypeName),
            }).ToList()
            : (await db.Subjects
                .Where(s => s.SpecializationId == specId && s.DeletedAt == null)
                .OrderBy(s => s.Code)
                .Select(s => new { s.SubjectId, s.Code, s.Name })
                .ToListAsync(ct))
                .Select(s => new { s.SubjectId, s.Code, s.Name, CohortNumber = (string?)null, IsFinalProject = false })
                .ToList();

        var uploads = await db.AssignmentUploads
            .Where(a => a.StudentEnrollmentId == enrollmentId && a.DeletedAt == null)
            .OrderBy(a => a.UploadedAt)
            .Select(a => new
            {
                a.AssignmentUploadId,
                a.SubjectId,
                a.Title,
                a.FileName,
                a.UploadedByRole,
                a.UploadedByName,
                a.UploadedAt,
                a.ProjectName,
                a.SupervisorName,
                a.WordCount,
                a.Plagiarism,
                Comments = a.Comments.OrderBy(c => c.CreatedAt).Select(c => new
                {
                    c.AssignmentCommentId,
                    c.AuthorRole,
                    c.AuthorName,
                    c.Text,
                    c.CreatedAt,
                }).ToList(),
            })
            .ToListAsync(ct);

        var bySubject = uploads.ToLookup(u => u.SubjectId);
        return new
        {
            modules = subjects.Select(s => new
            {
                subjectId = s.SubjectId,
                code = s.Code,
                name = s.Name,
                cohortNumber = s.CohortNumber,
                isFinalProject = s.IsFinalProject,
                uploads = bySubject[s.SubjectId].Select(u => new
                {
                    assignmentUploadId = u.AssignmentUploadId,
                    title = u.Title,
                    fileName = u.FileName,
                    uploadedByRole = u.UploadedByRole,
                    uploadedByName = u.UploadedByName,
                    uploadedAt = u.UploadedAt,
                    projectName = u.ProjectName,
                    supervisorName = u.SupervisorName,
                    wordCount = u.WordCount,
                    plagiarism = u.Plagiarism,
                    comments = u.Comments.Select(c => new
                    {
                        assignmentCommentId = c.AssignmentCommentId,
                        authorRole = c.AuthorRole,
                        authorName = c.AuthorName,
                        text = c.Text,
                        createdAt = c.CreatedAt,
                    }),
                }),
            }),
        };
    }

    /// <summary>"Dissertation Proposal" also contains "dissertation", so the
    /// proposal check comes first — same heuristic as the grade-save
    /// auto-release in ModuleCohortLogic.</summary>
    public static bool IsFinalProjectCohortType(string? typeName) =>
        !string.IsNullOrEmpty(typeName)
        && !typeName.Contains("proposal", StringComparison.OrdinalIgnoreCase)
        && (typeName.Contains("final project", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("dissertation", StringComparison.OrdinalIgnoreCase));

    public sealed record UploadInput(
        Guid SubjectId, string? Title,
        Stream Content, string? FileName, string? ContentType, long Length,
        string? ProjectName = null, string? SupervisorName = null, int? WordCount = null,
        string? Plagiarism = null);

    /// <summary>Stores the file and the upload row. Returns an error string
    /// (null on success) plus the created id.</summary>
    public async Task<(string? Error, Guid Id)> UploadAsync(
        Guid enrollmentId, UploadInput input,
        string uploaderRole, string? uploaderName, string? uploaderUserId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
            return ("A document title is required.", Guid.Empty);
        if (input.Length == 0)
            return ("file is required", Guid.Empty);
        if (input.Length > DocumentUploadPolicy.MaxBytes)
            return ($"file too large (max {DocumentUploadPolicy.MaxBytes / (1024 * 1024)} MB)", Guid.Empty);
        if (!DocumentUploadPolicy.IsAllowed(input.ContentType))
            return ($"only {DocumentUploadPolicy.AllowedHumanReadable} files are accepted", Guid.Empty);

        var specId = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => e.SpecializationId)
            .FirstOrDefaultAsync(ct);
        // Valid target: a module of the enrolment's specialization OR a
        // module the student is cohort-assigned to (cohorts may span specs).
        var subjectOk = await db.Subjects.AnyAsync(s =>
            s.SubjectId == input.SubjectId && s.SpecializationId == specId && s.DeletedAt == null, ct);
        if (!subjectOk)
            subjectOk = await (
                from mcs in db.ModuleCohortStudents
                join mc in db.ModuleCohorts on mcs.ModuleCohortId equals mc.ModuleCohortId
                where mcs.StudentEnrollmentId == enrollmentId && mcs.DeletedAt == null
                    && mc.DeletedAt == null && mc.SubjectId == input.SubjectId
                select mc.ModuleCohortId).AnyAsync(ct);
        if (!subjectOk)
            return ("That module doesn't belong to this enrolment.", Guid.Empty);

        var id = Guid.NewGuid();
        var safeName = Path.GetFileName(input.FileName ?? "assignment");
        var rel = $"assignments/{enrollmentId}/{id:N}-{safeName}";
        await storage.SaveAsync(input.Content, rel, ct);

        db.AssignmentUploads.Add(new AssignmentUpload
        {
            AssignmentUploadId = id,
            StudentEnrollmentId = enrollmentId,
            SubjectId = input.SubjectId,
            Title = input.Title.Trim(),
            FileName = safeName,
            MimeType = input.ContentType ?? "application/octet-stream",
            StoragePath = rel,
            UploadedByRole = uploaderRole,
            UploadedByName = uploaderName,
            UploadedByUserId = uploaderUserId,
            UploadedAt = DateTime.UtcNow,
            ProjectName = Trimmed(input.ProjectName),
            SupervisorName = Trimmed(input.SupervisorName),
            WordCount = input.WordCount,
            Plagiarism = Trimmed(input.Plagiarism),
        });
        await db.SaveChangesAsync(ct);
        return (null, id);
    }

    private static string? Trimmed(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Updates the final-project details on an existing upload
    /// (staff-only surfaces call this). Returns false when the upload does
    /// not belong to the enrolment.</summary>
    public async Task<bool> UpdateProjectFieldsAsync(
        Guid assignmentUploadId, Guid enrollmentId,
        string? projectName, string? supervisorName, int? wordCount,
        string? plagiarism,
        CancellationToken ct)
    {
        var upload = await db.AssignmentUploads.FirstOrDefaultAsync(a =>
            a.AssignmentUploadId == assignmentUploadId
            && a.StudentEnrollmentId == enrollmentId && a.DeletedAt == null, ct);
        if (upload is null) return false;
        upload.ProjectName = Trimmed(projectName);
        upload.SupervisorName = Trimmed(supervisorName);
        upload.WordCount = wordCount;
        upload.Plagiarism = Trimmed(plagiarism);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<object?> AddCommentAsync(
        Guid assignmentUploadId, Guid enrollmentId,
        string authorRole, string? authorName, string? authorUserId,
        string? text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var owned = await db.AssignmentUploads.AnyAsync(a =>
            a.AssignmentUploadId == assignmentUploadId
            && a.StudentEnrollmentId == enrollmentId && a.DeletedAt == null, ct);
        if (!owned) return null;

        var comment = new AssignmentComment
        {
            AssignmentUploadId = assignmentUploadId,
            AuthorRole = authorRole,
            AuthorName = authorName,
            AuthorUserId = authorUserId,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        db.AssignmentComments.Add(comment);
        await db.SaveChangesAsync(ct);
        return new
        {
            assignmentCommentId = comment.AssignmentCommentId,
            authorRole = comment.AuthorRole,
            authorName = comment.AuthorName,
            text = comment.Text,
            createdAt = comment.CreatedAt,
        };
    }

    public async Task<(Stream Stream, string FileName, string MimeType)?> OpenFileAsync(
        Guid assignmentUploadId, Guid enrollmentId, CancellationToken ct)
    {
        var meta = await db.AssignmentUploads
            .Where(a => a.AssignmentUploadId == assignmentUploadId
                && a.StudentEnrollmentId == enrollmentId && a.DeletedAt == null)
            .Select(a => new { a.StoragePath, a.FileName, a.MimeType })
            .FirstOrDefaultAsync(ct);
        if (meta is null) return null;
        var stream = await storage.OpenReadAsync(meta.StoragePath, ct);
        return (stream, meta.FileName, meta.MimeType);
    }
}

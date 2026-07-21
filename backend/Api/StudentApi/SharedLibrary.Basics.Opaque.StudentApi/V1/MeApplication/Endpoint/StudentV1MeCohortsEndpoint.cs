using System.Security.Claims;
using Odin.Api.Base.Storage;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Student portal Module Cohorts: the cohorts the student is assigned to
/// (per enrolment) — cohort number, module, schedule and teacher — plus
/// downloads of the upload fields flagged "visible to students" (e.g. the
/// Module Outline). Internal files (grading sheets, rubrics, QA) never leak.
/// </summary>
[Route("/v1/student/me/cohorts")]
[EndpointTag("Student.Me")]
public sealed class StudentV1MeCohortsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/enrollments/{enrollmentId:guid}/cohorts", ListAsync).RequireAuthorization();
        app.MapGet("/v1/student/me/enrollments/{enrollmentId:guid}/modules", ModulesAsync).RequireAuthorization();
        app.MapGet("/v1/student/me/cohort-files/{fileId:guid}/file", DownloadAsync).RequireAuthorization();
        app.MapGet("/v1/student/me/cohort-questionnaires/{id:guid}", GetQuestionnaireAsync).RequireAuthorization();
        app.MapPost("/v1/student/me/cohort-questionnaires/{id:guid}/submit", SubmitQuestionnaireAsync).RequireAuthorization();
        return app;
    }

    /// <summary>Resolves a cohort questionnaire the caller may fill: the
    /// student must be assigned to the questionnaire's cohort. Returns the
    /// enrolment used for the completion flag.</summary>
    private static async Task<(Guid QuestionnaireId, Guid TemplateId, Guid EnrollmentId)?> ResolveQuestionnaireAsync(
        Guid id, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return null;
        var hit = await (
            from q in db.ModuleCohortQuestionnaires
            join mcs in db.ModuleCohortStudents on q.ModuleCohortId equals mcs.ModuleCohortId
            join e in db.Enrollments on mcs.StudentEnrollmentId equals e.StudentEnrollmentId
            join s in db.Students on e.StudentId equals s.StudentId
            where q.ModuleCohortQuestionnaireId == id && q.DeletedAt == null
                && mcs.DeletedAt == null && e.DeletedAt == null
                && s.DeletedAt == null && s.UserId == callerId
            select new { q.ModuleCohortQuestionnaireId, q.QuestionnaireTemplateId, mcs.StudentEnrollmentId })
            .FirstOrDefaultAsync(ct);
        return hit is null ? null : (hit.ModuleCohortQuestionnaireId, hit.QuestionnaireTemplateId, hit.StudentEnrollmentId);
    }

    private static async Task<IResult> GetQuestionnaireAsync(
        Guid id, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var link = await ResolveQuestionnaireAsync(id, http, db, ct);
        if (link is not { } l) return Results.NotFound();

        var template = await db.QuestionnaireTemplates
            .Where(t => t.QuestionnaireTemplateId == l.TemplateId && t.DeletedAt == null)
            .Select(t => new { t.Name, t.DefinitionJson })
            .FirstOrDefaultAsync(ct);
        if (template is null) return Results.NotFound();

        var completed = await db.CohortQuestionnaireCompletions.AnyAsync(c =>
            c.ModuleCohortQuestionnaireId == l.QuestionnaireId
            && c.StudentEnrollmentId == l.EnrollmentId, ct);

        return Results.Ok(new
        {
            moduleCohortQuestionnaireId = l.QuestionnaireId,
            name = template.Name,
            definitionJson = template.DefinitionJson,
            completed,
        });
    }

    public sealed class SubmitBody
    {
        public string? AnswersJson { get; init; }
    }

    private static async Task<IResult> SubmitQuestionnaireAsync(
        Guid id, [FromBody] SubmitBody body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var link = await ResolveQuestionnaireAsync(id, http, db, ct);
        if (link is not { } l) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(body.AnswersJson))
            return Results.BadRequest(new { error = "answersJson is required." });

        var already = await db.CohortQuestionnaireCompletions.AnyAsync(c =>
            c.ModuleCohortQuestionnaireId == l.QuestionnaireId
            && c.StudentEnrollmentId == l.EnrollmentId, ct);
        if (already) return Results.BadRequest(new { error = "This questionnaire was already submitted." });

        var hash = await db.QuestionnaireTemplates
            .Where(t => t.QuestionnaireTemplateId == l.TemplateId)
            .Select(t => t.DefinitionHash)
            .FirstOrDefaultAsync(ct) ?? "";

        // The answers row carries no student reference (anonymous by design);
        // only the separate completion flag is per-student.
        db.CohortQuestionnaireResponses.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.CohortQuestionnaireResponse
        {
            ModuleCohortQuestionnaireId = l.QuestionnaireId,
            AnswersJson = body.AnswersJson,
            QuestionnaireVersionHash = hash,
        });
        db.CohortQuestionnaireCompletions.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.CohortQuestionnaireCompletion
        {
            ModuleCohortQuestionnaireId = l.QuestionnaireId,
            StudentEnrollmentId = l.EnrollmentId,
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { submitted = true });
    }

    /// <summary>Full module (subject) list of the enrolment's current
    /// specialization, with the student's cohort and score per module.
    /// Drives the Modules table on the portal's Programs tab.</summary>
    private static async Task<IResult> ModulesAsync(
        Guid enrollmentId, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (!await OwnsAsync(http, db, enrollmentId, ct)) return Results.NotFound();

        var specId = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => e.SpecializationId)
            .FirstOrDefaultAsync(ct);

        var modules = await db.Subjects
            .Where(s => s.SpecializationId == specId && s.DeletedAt == null)
            .OrderBy(s => s.Code)
            .Select(s => new
            {
                subjectId = s.SubjectId,
                code = s.Code,
                name = s.Name,
                ects = s.Ects,
                isThesis = s.IsThesis,
                score = db.SubjectGrades
                    .Where(g => g.StudentEnrollmentId == enrollmentId && g.SubjectId == s.SubjectId)
                    .Select(g => (int?)g.Score).FirstOrDefault(),
                cohortNumber = (
                    from mcs in db.ModuleCohortStudents
                    join mc in db.ModuleCohorts on mcs.ModuleCohortId equals mc.ModuleCohortId
                    where mcs.StudentEnrollmentId == enrollmentId && mcs.DeletedAt == null
                        && mc.DeletedAt == null && mc.SubjectId == s.SubjectId
                    select mc.CohortNumber).FirstOrDefault(),
            })
            .ToListAsync(ct);

        // Same grade gate as the cohorts list: modules whose cohort still has
        // unfilled questionnaires hide the score.
        var lockedSubjects = (await (
            from mcs in db.ModuleCohortStudents
            join mc in db.ModuleCohorts on mcs.ModuleCohortId equals mc.ModuleCohortId
            join q in db.ModuleCohortQuestionnaires on mc.ModuleCohortId equals q.ModuleCohortId
            where mcs.StudentEnrollmentId == enrollmentId && mcs.DeletedAt == null
                && mc.DeletedAt == null && q.DeletedAt == null
                && !db.CohortQuestionnaireCompletions.Any(cc =>
                    cc.ModuleCohortQuestionnaireId == q.ModuleCohortQuestionnaireId
                    && cc.StudentEnrollmentId == enrollmentId)
            select mc.SubjectId).Distinct().ToListAsync(ct)).ToHashSet();

        return Results.Ok(new
        {
            modules = modules.Select(m => new
            {
                m.subjectId,
                m.code,
                m.name,
                m.ects,
                m.isThesis,
                score = lockedSubjects.Contains(m.subjectId) ? null : m.score,
                gradeLocked = lockedSubjects.Contains(m.subjectId),
                m.cohortNumber,
            }).ToList(),
        });
    }

    private static async Task<bool> OwnsAsync(
        HttpContext http, OdinDbContext db, Guid enrollmentId, CancellationToken ct)
    {
        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return false;
        return await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.DeletedAt == null
            && db.Students.Any(s => s.StudentId == e.StudentId && s.UserId == callerId && s.DeletedAt == null), ct);
    }

    private static async Task<IResult> ListAsync(
        Guid enrollmentId, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (!await OwnsAsync(http, db, enrollmentId, ct)) return Results.NotFound();

        var cohorts = await (
            from mcs in db.ModuleCohortStudents
            join c in db.ModuleCohorts on mcs.ModuleCohortId equals c.ModuleCohortId
            where mcs.StudentEnrollmentId == enrollmentId && mcs.DeletedAt == null && c.DeletedAt == null
            select new
            {
                c.ModuleCohortId,
                c.CohortNumber,
                c.StartDate,
                c.EndDate,
                c.SubjectId,
                ModuleCode = db.Subjects.Where(s => s.SubjectId == c.SubjectId).Select(s => s.Code).FirstOrDefault(),
                ModuleName = db.Subjects.Where(s => s.SubjectId == c.SubjectId).Select(s => s.Name).FirstOrDefault(),
                TeacherName = db.Teachers.Where(t => t.TeacherId == c.TeacherId).Select(t => t.DisplayName).FirstOrDefault(),
                // Same visibility rule as the provisional transcript: a score
                // is shown as soon as it has been entered for this enrolment.
                Score = db.SubjectGrades
                    .Where(g => g.StudentEnrollmentId == enrollmentId && g.SubjectId == c.SubjectId)
                    .Select(g => (int?)g.Score).FirstOrDefault(),
                GradedAt = db.SubjectGrades
                    .Where(g => g.StudentEnrollmentId == enrollmentId && g.SubjectId == c.SubjectId)
                    .Select(g => g.GradedAt).FirstOrDefault(),
            }).ToListAsync(ct);
        var cohortIds = cohorts.Select(c => c.ModuleCohortId).ToList();

        var files = await (
            from f in db.CohortUploadFiles
            join fld in db.CohortUploadFields on f.CohortUploadFieldId equals fld.CohortUploadFieldId
            where cohortIds.Contains(f.ModuleCohortId) && f.DeletedAt == null
                && fld.DeletedAt == null && fld.VisibleToStudents
            select new { f.ModuleCohortId, f.CohortUploadFileId, f.FileName, FieldLabel = fld.Label })
            .ToListAsync(ct);

        // Grade gate: every questionnaire attached to the cohort must be
        // submitted by this student before the score is revealed.
        var cohortQuestionnaires = await (
            from q in db.ModuleCohortQuestionnaires
            join t in db.QuestionnaireTemplates on q.QuestionnaireTemplateId equals t.QuestionnaireTemplateId
            where cohortIds.Contains(q.ModuleCohortId) && q.DeletedAt == null && t.DeletedAt == null
            orderby q.SortOrder, q.CreatedAt
            select new { q.ModuleCohortId, q.ModuleCohortQuestionnaireId, t.Name })
            .ToListAsync(ct);
        var completedSet = (await db.CohortQuestionnaireCompletions
                .Where(c => c.StudentEnrollmentId == enrollmentId)
                .Select(c => c.ModuleCohortQuestionnaireId)
                .ToListAsync(ct)).ToHashSet();

        return Results.Ok(new
        {
            cohorts = cohorts.OrderBy(c => c.StartDate).Select(c =>
            {
                var qs = cohortQuestionnaires
                    .Where(q => q.ModuleCohortId == c.ModuleCohortId)
                    .Select(q => new
                    {
                        id = q.ModuleCohortQuestionnaireId,
                        name = q.Name,
                        completed = completedSet.Contains(q.ModuleCohortQuestionnaireId),
                    }).ToList();
                var gradeLocked = qs.Any(q => !q.completed);
                return new
                {
                    moduleCohortId = c.ModuleCohortId,
                    cohortNumber = c.CohortNumber,
                    subjectId = c.SubjectId,
                    moduleCode = c.ModuleCode,
                    moduleName = c.ModuleName,
                    teacherName = c.TeacherName,
                    startDate = c.StartDate,
                    endDate = c.EndDate,
                    score = gradeLocked ? null : c.Score,
                    gradedAt = gradeLocked ? null : c.GradedAt,
                    gradeLocked,
                    questionnaires = qs,
                    files = files.Where(f => f.ModuleCohortId == c.ModuleCohortId)
                        .Select(f => new { id = f.CohortUploadFileId, fileName = f.FileName, fieldLabel = f.FieldLabel })
                        .ToList(),
                };
            }).ToList(),
        });
    }

    private static async Task<IResult> DownloadAsync(
        Guid fileId, HttpContext http, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.NotFound();

        // The file must be student-visible AND on a cohort this student is in.
        var file = await (
            from f in db.CohortUploadFiles
            join fld in db.CohortUploadFields on f.CohortUploadFieldId equals fld.CohortUploadFieldId
            where f.CohortUploadFileId == fileId && f.DeletedAt == null
                && fld.DeletedAt == null && fld.VisibleToStudents
                && db.ModuleCohortStudents.Any(mcs => mcs.ModuleCohortId == f.ModuleCohortId && mcs.DeletedAt == null
                    && db.Enrollments.Any(e => e.StudentEnrollmentId == mcs.StudentEnrollmentId && e.DeletedAt == null
                        && db.Students.Any(s => s.StudentId == e.StudentId && s.UserId == callerId && s.DeletedAt == null)))
            select new { f.StoragePath, f.FileName }).FirstOrDefaultAsync(ct);
        if (file is null) return Results.NotFound();
        try
        {
            var stream = await storage.OpenReadAsync(file.StoragePath, ct);
            return Results.File(stream, "application/octet-stream", file.FileName);
        }
        catch (FileNotFoundException) { return Results.NotFound(); }
    }
}

using Odin.Api.Base.Storage;
using School.PartnerAdminApi.Admin.V1.ModuleCohorts;
using School.PartnerAdminApi.Partner.V1.MyUsers;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Partner.V1.MyCohorts;

/// <summary>
/// Partner-portal Module Cohort Schedule: partner admins create/edit their
/// cohorts, upload materials and grading sheets, tick QA and assign
/// students. Teacher users see ONLY the cohorts assigned to them and are
/// fully read-only (writes are blocked by the teacher write-gate anyway).
/// </summary>
[Route("/v1/partner/my/cohorts")]
[EndpointTag("Partner.MyCohorts")]
public sealed class PartnerV1MyCohortsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my/cohorts", ListAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/cohort-sources", SourcesAsync).RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my/cohorts", CreateAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/cohorts/{cohortId:guid}", GetAsync).RequireAuthorization("PartnerOnly");
        app.MapPut("/v1/partner/my/cohorts/{cohortId:guid}", UpdateAsync).RequireAuthorization("PartnerOnly");
        app.MapDelete("/v1/partner/my/cohorts/{cohortId:guid}", DeleteAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/cohorts/{cohortId:guid}/students", StudentsAsync).RequireAuthorization("PartnerOnly");
        app.MapPut("/v1/partner/my/cohorts/{cohortId:guid}/students", AssignStudentsAsync).RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my/cohorts/{cohortId:guid}/files", UploadFilesAsync)
            .RequireAuthorization("PartnerOnly").DisableAntiforgery();
        app.MapDelete("/v1/partner/my/cohort-files/{fileId:guid}", DeleteFileAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/cohort-files/{fileId:guid}/file", DownloadFileAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/cohorts/{cohortId:guid}/grades", GradesAsync).RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my/cohorts/{cohortId:guid}/grades/draft", SaveGradesDraftAsync).RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my/cohorts/{cohortId:guid}/grades/submit", SubmitGradesAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/cohorts", StudentCohortsAsync).RequireAuthorization("PartnerOnly");
        app.MapPut("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/cohorts", SetStudentCohortAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static DateTime? Norm(DateTime? d) =>
        d is { } x ? DateTime.SpecifyKind(x.Date, DateTimeKind.Unspecified) : null;

    /// <summary>(callerUserId, partnerId, teacherIdIfTeacherUser, failure)</summary>
    private static async Task<(string? CallerId, Guid? PartnerId, Guid? TeacherId, IResult? Fail)> ResolveAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (callerId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return (null, null, null, fail ?? Results.StatusCode(403));
        var isTeacher = await db.Users
            .Where(u => u.Id == callerId)
            .Select(u => u.IsTeacher)
            .FirstOrDefaultAsync(ct);
        Guid? teacherId = null;
        if (isTeacher)
        {
            teacherId = await db.Teachers
                .Where(t => t.UserId == callerId && t.DeletedAt == null)
                .Select(t => (Guid?)t.TeacherId)
                .FirstOrDefaultAsync(ct);
            // A teacher login without a teacher record sees nothing.
            teacherId ??= Guid.Empty;
        }
        return (callerId, partnerId, teacherId, null);
    }

    private static async Task<bool> OwnedAsync(OdinDbContext db, Guid cohortId, Guid partnerId, CancellationToken ct) =>
        await db.ModuleCohorts.AnyAsync(c =>
            c.ModuleCohortId == cohortId && c.PartnerId == partnerId && c.DeletedAt == null, ct);

    private static async Task<IResult> ListAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, teacherId, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var items = await ModuleCohortLogic.ListAsync(db, partnerId, teacherId, ct);
        return Results.Ok(new { items, isTeacherView = teacherId is not null });
    }

    private static async Task<IResult> SourcesAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, _, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var grantedIds = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partnerId && pp.IsActive != null)
            .Select(pp => pp.ProgrammeId)
            .ToListAsync(ct);
        var programmes = await db.Programmes
            .Where(p => p.DeletedAt == null && (grantedIds.Contains(p.ProgrammeId) || p.OwnerId == partnerId))
            .OrderBy(p => p.Name)
            .Select(p => new { programmeId = p.ProgrammeId, code = p.Code, name = p.Name })
            .ToListAsync(ct);
        var programmeIds = programmes.Select(p => p.programmeId).ToList();
        var modules = await (
            from s in db.Subjects
            join sp in db.Specializations on s.SpecializationId equals sp.SpecializationId
            where s.DeletedAt == null && programmeIds.Contains(sp.ProgrammeId)
            orderby s.Code
            select new
            {
                subjectId = s.SubjectId,
                programmeId = sp.ProgrammeId,
                specializationName = sp.Name,
                code = s.Code,
                name = s.Name,
            }).ToListAsync(ct);
        var teachers = await db.Teachers
            .Where(t => t.PartnerId == partnerId && t.DeletedAt == null)
            .OrderBy(t => t.DisplayName)
            .Select(t => new { teacherId = t.TeacherId, displayName = t.DisplayName })
            .ToListAsync(ct);
        return Results.Ok(new { programmes, modules, teachers });
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext, [FromBody] AdminV1ModuleCohortsEndpoint.CreateBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, _, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        if (body.ProgrammeId is null || body.SubjectId is null)
            return Results.BadRequest(new { error = "Programme and module are required." });
        var moduleOk = await (
            from s in db.Subjects
            join sp in db.Specializations on s.SpecializationId equals sp.SpecializationId
            where s.SubjectId == body.SubjectId && sp.ProgrammeId == body.ProgrammeId && s.DeletedAt == null
            select s.SubjectId).AnyAsync(ct);
        if (!moduleOk) return Results.BadRequest(new { error = "That module does not belong to the chosen programme." });
        if (body.TeacherId is not null)
        {
            var teacherOk = await db.Teachers.AnyAsync(t =>
                t.TeacherId == body.TeacherId && t.PartnerId == partnerId && t.DeletedAt == null, ct);
            if (!teacherOk) return Results.BadRequest(new { error = "Unknown teacher." });
        }

        var cohort = new ModuleCohort
        {
            PartnerId = partnerId!.Value,
            ProgrammeId = body.ProgrammeId.Value,
            SubjectId = body.SubjectId.Value,
            TeacherId = body.TeacherId,
            StartDate = Norm(body.StartDate),
            EndDate = Norm(body.EndDate),
            CohortNumber = await ModuleCohortLogic.NextCohortNumberAsync(db, partnerId.Value, body.SubjectId.Value, ct),
        };
        db.ModuleCohorts.Add(cohort);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { moduleCohortId = cohort.ModuleCohortId, cohortNumber = cohort.CohortNumber });
    }

    private static async Task<IResult> GetAsync(
        Guid cohortId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, teacherId, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        if (!await OwnedAsync(db, cohortId, partnerId!.Value, ct)) return Results.NotFound();
        if (teacherId is not null)
        {
            var mine = await db.ModuleCohorts.AnyAsync(c =>
                c.ModuleCohortId == cohortId && c.TeacherId == teacherId, ct);
            if (!mine) return Results.NotFound();
        }
        var detail = await ModuleCohortLogic.DetailAsync(db, cohortId, ct);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    }

    private static async Task<IResult> UpdateAsync(
        Guid cohortId, HttpContext httpContext,
        [FromBody] AdminV1ModuleCohortsEndpoint.UpdateBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, _, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var cohort = await db.ModuleCohorts.FirstOrDefaultAsync(c =>
            c.ModuleCohortId == cohortId && c.PartnerId == partnerId && c.DeletedAt == null, ct);
        if (cohort is null) return Results.NotFound();

        if (body.TeacherId is not null)
        {
            var teacherOk = await db.Teachers.AnyAsync(t =>
                t.TeacherId == body.TeacherId && t.PartnerId == partnerId && t.DeletedAt == null, ct);
            if (!teacherOk) return Results.BadRequest(new { error = "Unknown teacher." });
        }
        cohort.TeacherId = body.TeacherId;
        cohort.StartDate = Norm(body.StartDate);
        var oldEnd = cohort.EndDate;
        cohort.EndDate = Norm(body.EndDate);
        if (oldEnd != cohort.EndDate)
        {
            cohort.Reminder2WeeksSent = false;
            cohort.Reminder1WeekSent = false;
            cohort.ReminderOverdueSent = false;
        }
        cohort.GradingSheetDueOverride = body.ClearDueOverride ? null : (Norm(body.GradingSheetDueOverride) ?? cohort.GradingSheetDueOverride);
        cohort.GradingSheetUploadedDate = Norm(body.GradingSheetUploadedDate);
        if (body.DocQaChecked is { } dq) cohort.DocQaChecked = dq;
        cohort.DocQaDate = Norm(body.DocQaDate);
        if (body.GradeQaChecked is { } gq) cohort.GradeQaChecked = gq;
        cohort.GradeQaDate = Norm(body.GradeQaDate);
        cohort.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> DeleteAsync(
        Guid cohortId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, _, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var cohort = await db.ModuleCohorts.FirstOrDefaultAsync(c =>
            c.ModuleCohortId == cohortId && c.PartnerId == partnerId && c.DeletedAt == null, ct);
        if (cohort is null) return Results.NotFound();
        cohort.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static async Task<IResult> StudentsAsync(
        Guid cohortId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, _, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        if (!await OwnedAsync(db, cohortId, partnerId!.Value, ct)) return Results.NotFound();

        var cohort = await db.ModuleCohorts
            .Where(c => c.ModuleCohortId == cohortId)
            .Select(c => new { c.ProgrammeId })
            .FirstAsync(ct);
        var assignedIds = (await db.ModuleCohortStudents
            .Where(s => s.ModuleCohortId == cohortId && s.DeletedAt == null)
            .Select(s => s.StudentEnrollmentId)
            .ToListAsync(ct)).ToHashSet();
        var candidates = await db.Enrollments
            .Where(e => e.DeletedAt == null && e.PartnerId == partnerId
                && e.Specialization.ProgrammeId == cohort.ProgrammeId
                && ModuleCohortLogic.AssignableStatusCodes.Contains(e.Status.Code))
            .Select(e => new
            {
                enrollmentId = e.StudentEnrollmentId,
                studentId = e.StudentId,
                statusName = e.Status.Name,
                studentNumber = db.Students.Where(s => s.StudentId == e.StudentId).Select(s => s.StudentNumber).FirstOrDefault(),
                firstName = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => db.UserProfiles.Where(p => p.UserId == s.UserId).Select(p => p.FirstName).FirstOrDefault()).FirstOrDefault(),
                lastName = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => db.UserProfiles.Where(p => p.UserId == s.UserId).Select(p => p.LastName).FirstOrDefault()).FirstOrDefault(),
            })
            .ToListAsync(ct);
        return Results.Ok(new
        {
            students = candidates.Select(c => new
            {
                c.enrollmentId,
                c.studentId,
                c.studentNumber,
                c.firstName,
                c.lastName,
                c.statusName,
                assigned = assignedIds.Contains(c.enrollmentId),
            }).OrderBy(c => c.lastName).ThenBy(c => c.firstName).ToList(),
        });
    }

    private static async Task<IResult> AssignStudentsAsync(
        Guid cohortId, HttpContext httpContext,
        [FromBody] AdminV1ModuleCohortsEndpoint.AssignBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, _, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var cohort = await db.ModuleCohorts.FirstOrDefaultAsync(c =>
            c.ModuleCohortId == cohortId && c.PartnerId == partnerId && c.DeletedAt == null, ct);
        if (cohort is null) return Results.NotFound();

        var wanted = (body.EnrollmentIds ?? []).ToHashSet();
        var existing = await db.ModuleCohortStudents
            .Where(s => s.ModuleCohortId == cohortId)
            .ToListAsync(ct);
        foreach (var row in existing)
            row.DeletedAt = wanted.Contains(row.StudentEnrollmentId) ? null : (row.DeletedAt ?? DateTime.UtcNow);
        var have = existing.Select(r => r.StudentEnrollmentId).ToHashSet();
        foreach (var id in wanted.Where(id => !have.Contains(id)))
            db.ModuleCohortStudents.Add(new ModuleCohortStudent { ModuleCohortId = cohortId, StudentEnrollmentId = id });
        cohort.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> UploadFilesAsync(
        Guid cohortId, [FromQuery] Guid fieldId, IFormFileCollection files,
        HttpContext httpContext, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var (_, partnerId, teacherId, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var cohort = await db.ModuleCohorts.FirstOrDefaultAsync(c =>
            c.ModuleCohortId == cohortId && c.PartnerId == partnerId && c.DeletedAt == null, ct);
        if (cohort is null) return Results.NotFound();
        // Teachers may upload only on their OWN cohorts.
        if (teacherId is not null && cohort.TeacherId != teacherId) return Results.NotFound();
        var field = await db.CohortUploadFields
            .FirstOrDefaultAsync(f => f.CohortUploadFieldId == fieldId && f.DeletedAt == null, ct);
        if (field is null) return Results.BadRequest(new { error = "Unknown upload field." });
        if (files is null || files.Count == 0) return Results.BadRequest(new { error = "No files supplied." });
        if (!field.AllowMultiple && files.Count > 1)
            return Results.BadRequest(new { error = $"\"{field.Label}\" accepts a single document." });
        if (files.Any(f => f.Length > 100 * 1024 * 1024))
            return Results.BadRequest(new { error = "Max file size is 100 MB." });

        if (!field.AllowMultiple)
        {
            var olds = await db.CohortUploadFiles
                .Where(x => x.ModuleCohortId == cohortId && x.CohortUploadFieldId == fieldId && x.DeletedAt == null)
                .ToListAsync(ct);
            foreach (var old in olds) old.DeletedAt = DateTime.UtcNow;
        }
        foreach (var file in files)
        {
            var safeName = Path.GetFileName(file.FileName);
            string storagePath;
            await using (var stream = file.OpenReadStream())
            {
                storagePath = await storage.SaveAsync(
                    stream, $"{ModuleCohortLogic.StoragePrefix}{cohortId:N}/{Guid.NewGuid():N}-{safeName}", ct);
            }
            db.CohortUploadFiles.Add(new CohortUploadFile
            {
                ModuleCohortId = cohortId,
                CohortUploadFieldId = fieldId,
                FileName = safeName,
                StoragePath = storagePath,
            });
        }
        if (field.IsGradingSheet && cohort.GradingSheetUploadedDate is null)
            cohort.GradingSheetUploadedDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
        cohort.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { uploaded = files.Count });
    }

    private static async Task<IResult> DeleteFileAsync(
        Guid fileId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, teacherId, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var file = await (
            from f in db.CohortUploadFiles
            join c in db.ModuleCohorts on f.ModuleCohortId equals c.ModuleCohortId
            where f.CohortUploadFileId == fileId && f.DeletedAt == null && c.PartnerId == partnerId
                && (teacherId == null || c.TeacherId == teacherId)
            select f).FirstOrDefaultAsync(ct);
        if (file is null) return Results.NotFound();
        file.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static async Task<IResult> DownloadFileAsync(
        Guid fileId, HttpContext httpContext, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var (_, partnerId, _, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var file = await (
            from f in db.CohortUploadFiles
            join c in db.ModuleCohorts on f.ModuleCohortId equals c.ModuleCohortId
            where f.CohortUploadFileId == fileId && f.DeletedAt == null && c.PartnerId == partnerId
            select new { f.StoragePath, f.FileName }).FirstOrDefaultAsync(ct);
        if (file is null) return Results.NotFound();
        try
        {
            var stream = await storage.OpenReadAsync(file.StoragePath, ct);
            return Results.File(stream, "application/octet-stream", file.FileName);
        }
        catch (FileNotFoundException) { return Results.NotFound(); }
    }

    private static async Task<IResult> GradesAsync(
        Guid cohortId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, teacherId, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var owned = await db.ModuleCohorts.AnyAsync(c =>
            c.ModuleCohortId == cohortId && c.PartnerId == partnerId && c.DeletedAt == null
            && (teacherId == null || c.TeacherId == teacherId), ct);
        if (!owned) return Results.NotFound();
        var result = await ModuleCohortLogic.GradesAsync(db, cohortId, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> SaveGradesDraftAsync(
        Guid cohortId, HttpContext httpContext,
        [FromBody] ModuleCohortLogic.GradesDraftBody body, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, teacherId, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var owned = await db.ModuleCohorts.AnyAsync(c =>
            c.ModuleCohortId == cohortId && c.PartnerId == partnerId && c.DeletedAt == null
            && (teacherId == null || c.TeacherId == teacherId), ct);
        if (!owned) return Results.NotFound();
        var (found, error, saved, skipped) = await ModuleCohortLogic.SaveGradesDraftAsync(db, cohortId, body, ct);
        if (!found) return Results.NotFound();
        return error is null ? Results.Ok(new { saved, skipped }) : Results.BadRequest(new { error });
    }

    /// <summary>Partner-admin submit (teachers draft only — the write-gate
    /// blocks them here, and the ownership check double-locks it).</summary>
    private static async Task<IResult> SubmitGradesAsync(
        Guid cohortId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (callerId, partnerId, teacherId, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        if (teacherId is not null)
            return Results.StatusCode(403); // teachers may draft, not submit
        var owned = await db.ModuleCohorts.AnyAsync(c =>
            c.ModuleCohortId == cohortId && c.PartnerId == partnerId && c.DeletedAt == null, ct);
        if (!owned) return Results.NotFound();
        Guid.TryParse(callerId, out var byUserId);
        var result = await ModuleCohortLogic.SubmitGradesAsync(db, cohortId, byUserId, "Partner", ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> StudentCohortsAsync(
        Guid studentId, Guid enrollmentId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, _, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var enrolment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId
                && e.PartnerId == partnerId && e.DeletedAt == null)
            .Select(e => new { e.SpecializationId })
            .FirstOrDefaultAsync(ct);
        if (enrolment is null) return Results.NotFound();

        var modules = await db.Subjects
            .Where(s => s.SpecializationId == enrolment.SpecializationId && s.DeletedAt == null)
            .OrderBy(s => s.Code)
            .Select(s => new { s.SubjectId, s.Code, s.Name })
            .ToListAsync(ct);
        var subjectIds = modules.Select(m => m.SubjectId).ToList();
        var cohorts = await db.ModuleCohorts
            .Where(c => c.DeletedAt == null && c.PartnerId == partnerId && subjectIds.Contains(c.SubjectId))
            .Select(c => new { c.ModuleCohortId, c.SubjectId, c.CohortNumber, c.StartDate, c.EndDate })
            .ToListAsync(ct);
        var cohortIds = cohorts.Select(c => c.ModuleCohortId).ToList();
        var assignedSet = (await db.ModuleCohortStudents
            .Where(s => s.StudentEnrollmentId == enrollmentId && s.DeletedAt == null && cohortIds.Contains(s.ModuleCohortId))
            .Select(s => s.ModuleCohortId)
            .ToListAsync(ct)).ToHashSet();

        return Results.Ok(new
        {
            modules = modules.Select(m => new
            {
                subjectId = m.SubjectId,
                code = m.Code,
                name = m.Name,
                cohorts = cohorts.Where(c => c.SubjectId == m.SubjectId)
                    .Select(c => new
                    {
                        moduleCohortId = c.ModuleCohortId,
                        cohortNumber = c.CohortNumber,
                        startDate = c.StartDate,
                        endDate = c.EndDate,
                    }).ToList(),
                assignedCohortId = cohorts
                    .Where(c => c.SubjectId == m.SubjectId && assignedSet.Contains(c.ModuleCohortId))
                    .Select(c => (Guid?)c.ModuleCohortId)
                    .FirstOrDefault(),
            }).ToList(),
        });
    }

    private static async Task<IResult> SetStudentCohortAsync(
        Guid studentId, Guid enrollmentId, HttpContext httpContext,
        [FromBody] AdminV1ModuleCohortsEndpoint.SetStudentCohortBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, _, fail) = await ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var owned = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId
            && e.PartnerId == partnerId && e.DeletedAt == null, ct);
        if (!owned) return Results.NotFound();

        var sameModuleCohortIds = await db.ModuleCohorts
            .Where(c => c.PartnerId == partnerId && c.SubjectId == body.SubjectId)
            .Select(c => c.ModuleCohortId)
            .ToListAsync(ct);
        var rows = await db.ModuleCohortStudents
            .Where(s => s.StudentEnrollmentId == enrollmentId && sameModuleCohortIds.Contains(s.ModuleCohortId))
            .ToListAsync(ct);
        foreach (var row in rows)
            row.DeletedAt = row.ModuleCohortId == body.CohortId ? null : (row.DeletedAt ?? DateTime.UtcNow);
        if (body.CohortId is { } target && rows.All(r => r.ModuleCohortId != target))
        {
            var valid = await db.ModuleCohorts.AnyAsync(c =>
                c.ModuleCohortId == target && c.PartnerId == partnerId
                && c.SubjectId == body.SubjectId && c.DeletedAt == null, ct);
            if (!valid) return Results.BadRequest(new { error = "Unknown cohort for this module." });
            db.ModuleCohortStudents.Add(new ModuleCohortStudent
            {
                ModuleCohortId = target,
                StudentEnrollmentId = enrollmentId,
            });
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }
}

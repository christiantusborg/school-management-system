using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.ModuleCohorts;

/// <summary>
/// Shared logic of the Module Cohort Schedule feature: cohort-number
/// generation from the configurable pattern, effective grading-sheet due
/// date (EndDate + 1 month unless overridden), list/detail projections and
/// the set of enrolment statuses that count as "admitted/active" for
/// assignment.
/// </summary>
public static class ModuleCohortLogic
{
    public const string StoragePrefix = "module-cohorts/";

    /// <summary>Only admitted/active students may be placed in a cohort.</summary>
    public static readonly string[] AssignableStatusCodes =
    [
        "ApplicationApprovedAdmission", "AcceptAdmission",
        "AwaitingGradesSubmit", "AwaitingGradesApproval", "GradesApproved",
    ];

    public static DateTime? EffectiveDueDate(ModuleCohort c) =>
        c.GradingSheetDueOverride ?? c.EndDate?.AddMonths(1);

    public static async Task<ModuleCohortSettings> SettingsAsync(OdinDbContext db, CancellationToken ct)
    {
        var settings = await db.ModuleCohortSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new ModuleCohortSettings();
            db.ModuleCohortSettings.Add(settings);
            await db.SaveChangesAsync(ct);
        }
        return settings;
    }

    /// <summary>
    /// Generates the next cohort number: {partner} = partner name condensed,
    /// {module} = module code condensed, {n} = next 3-digit sequence per
    /// (partner, module) — numbers are never reissued.
    /// </summary>
    public static async Task<string> NextCohortNumberAsync(
        OdinDbContext db, Guid partnerId, Guid subjectId, CancellationToken ct)
    {
        var settings = await SettingsAsync(db, ct);
        var partnerName = await db.Partners
            .Where(p => p.PartnerId == partnerId).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "PARTNER";
        var moduleCode = await db.Subjects
            .Where(s => s.SubjectId == subjectId).Select(s => s.Code).FirstOrDefaultAsync(ct) ?? "MODULE";

        static string Condense(string s)
        {
            var t = new string(s.Where(char.IsLetterOrDigit).ToArray());
            return t.Length > 0 ? t : "X";
        }

        var issued = await db.ModuleCohorts
            .IgnoreQueryFilters()
            .Where(c => c.PartnerId == partnerId && c.SubjectId == subjectId)
            .Select(c => c.CohortNumber)
            .ToListAsync(ct);
        var max = 0;
        foreach (var number in issued)
        {
            var tail = number.Split('-').LastOrDefault();
            if (int.TryParse(tail, out var n) && n > max) max = n;
        }

        return (settings.CohortNumberPattern ?? "{partner}-{module}-{n}")
            .Replace("{partner}", Condense(partnerName), StringComparison.OrdinalIgnoreCase)
            .Replace("{module}", Condense(moduleCode), StringComparison.OrdinalIgnoreCase)
            .Replace("{n}", (max + 1).ToString("D3"), StringComparison.OrdinalIgnoreCase);
    }

    public sealed class GradeItemDto
    {
        public Guid EnrollmentId { get; init; }
        public int? Score { get; init; }
    }

    public sealed class GradesDraftBody
    {
        public List<GradeItemDto>? Items { get; init; }
    }

    /// <summary>The cohort's assigned students with their current mark for
    /// this cohort's module (name, number, status, score).</summary>
    public static async Task<object?> GradesAsync(OdinDbContext db, Guid cohortId, CancellationToken ct)
    {
        var cohort = await db.ModuleCohorts
            .Where(c => c.ModuleCohortId == cohortId && c.DeletedAt == null)
            .Select(c => new { c.SubjectId, c.CohortNumber })
            .FirstOrDefaultAsync(ct);
        if (cohort is null) return null;

        var rows = await (
            from mcs in db.ModuleCohortStudents
            join e in db.Enrollments on mcs.StudentEnrollmentId equals e.StudentEnrollmentId
            where mcs.ModuleCohortId == cohortId && mcs.DeletedAt == null && e.DeletedAt == null
            select new
            {
                enrollmentId = e.StudentEnrollmentId,
                statusName = e.Status.Name,
                statusCode = e.Status.Code,
                studentNumber = db.Students.Where(s => s.StudentId == e.StudentId).Select(s => s.StudentNumber).FirstOrDefault(),
                firstName = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => db.UserProfiles.Where(pr => pr.UserId == s.UserId).Select(pr => pr.FirstName).FirstOrDefault()).FirstOrDefault(),
                lastName = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => db.UserProfiles.Where(pr => pr.UserId == s.UserId).Select(pr => pr.LastName).FirstOrDefault()).FirstOrDefault(),
                score = db.SubjectGrades
                    .Where(g => g.StudentEnrollmentId == e.StudentEnrollmentId && g.SubjectId == cohort.SubjectId)
                    .Select(g => (int?)g.Score).FirstOrDefault(),
            }).ToListAsync(ct);

        return new
        {
            cohortNumber = cohort.CohortNumber,
            students = rows.OrderBy(r => r.lastName).ThenBy(r => r.firstName).ToList(),
        };
    }

    /// <summary>Draft-saves marks for the cohort's module into the normal
    /// grade sheet (SubjectGrades). Never touches enrolment status.</summary>
    public static async Task<(bool Found, string? Error, int Saved)> SaveGradesDraftAsync(
        OdinDbContext db, Guid cohortId, GradesDraftBody body, CancellationToken ct)
    {
        var cohort = await db.ModuleCohorts
            .Where(c => c.ModuleCohortId == cohortId && c.DeletedAt == null)
            .Select(c => new { c.SubjectId })
            .FirstOrDefaultAsync(ct);
        if (cohort is null) return (false, null, 0);

        var assignedIds = (await db.ModuleCohortStudents
            .Where(s => s.ModuleCohortId == cohortId && s.DeletedAt == null)
            .Select(s => s.StudentEnrollmentId)
            .ToListAsync(ct)).ToHashSet();

        var items = (body.Items ?? []).Where(i => i.Score is not null).ToList();
        foreach (var item in items)
        {
            if (!assignedIds.Contains(item.EnrollmentId))
                return (true, "A student in the payload is not assigned to this cohort.", 0);
            if (item.Score is < 0 or > 100)
                return (true, "Score must be between 0 and 100.", 0);
        }

        var enrollmentIds = items.Select(i => i.EnrollmentId).ToList();
        var existing = await db.SubjectGrades
            .Where(g => enrollmentIds.Contains(g.StudentEnrollmentId) && g.SubjectId == cohort.SubjectId)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            var row = existing.FirstOrDefault(g => g.StudentEnrollmentId == item.EnrollmentId);
            if (row is not null) { row.Score = item.Score!.Value; row.GradedAt = now; }
            else db.SubjectGrades.Add(new SharedLibrary.Basics.Opaque.Domains.SubjectGrade
            {
                StudentEnrollmentId = item.EnrollmentId,
                SubjectId = cohort.SubjectId,
                Score = item.Score!.Value,
                GradedAt = now,
            });
        }
        await db.SaveChangesAsync(ct);
        return (true, null, items.Count);
    }

    /// <summary>
    /// Submit for approval: flips each assigned enrolment that is in a
    /// pre-approval status AND meets the programme's ECTS completion gate to
    /// AwaitingGradesApproval (same rule as the per-student submit). Returns
    /// submitted count + per-student skip reasons.
    /// </summary>
    public static async Task<object?> SubmitGradesAsync(
        OdinDbContext db, Guid cohortId, Guid byUserId, string actorLabel, CancellationToken ct)
    {
        var cohort = await db.ModuleCohorts
            .Where(c => c.ModuleCohortId == cohortId && c.DeletedAt == null)
            .Select(c => new { c.CohortNumber })
            .FirstOrDefaultAsync(ct);
        if (cohort is null) return null;

        var allowedFrom = new HashSet<Guid>
        {
            SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.AcceptOffer,
            SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.ApplicationApprovedAdmission,
            SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.AcceptAdmission,
            SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.AwaitingGradesSubmit,
        };

        var enrolments = await (
            from mcs in db.ModuleCohortStudents
            join e in db.Enrollments on mcs.StudentEnrollmentId equals e.StudentEnrollmentId
            where mcs.ModuleCohortId == cohortId && mcs.DeletedAt == null && e.DeletedAt == null
            select e).ToListAsync(ct);

        var submitted = 0;
        var skipped = new List<object>();
        var now = DateTime.UtcNow;
        foreach (var enrolment in enrolments)
        {
            var studentNumber = await db.Students
                .Where(s => s.StudentId == enrolment.StudentId)
                .Select(s => s.StudentNumber).FirstOrDefaultAsync(ct) ?? "?";
            if (!allowedFrom.Contains(enrolment.StatusId))
            {
                skipped.Add(new { studentNumber, reason = "Enrolment is not in a grade-submission status." });
                continue;
            }
            var specSubjects = await db.Subjects
                .Where(s => s.SpecializationId == enrolment.SpecializationId && s.DeletedAt == null)
                .Select(s => new { s.SubjectId, s.Ects })
                .ToListAsync(ct);
            var gradedIds = (await db.SubjectGrades
                .Where(g => g.StudentEnrollmentId == enrolment.StudentEnrollmentId)
                .Select(g => g.SubjectId).ToListAsync(ct)).ToHashSet();
            var requiredEcts = await db.Enrollments
                .Where(e => e.StudentEnrollmentId == enrolment.StudentEnrollmentId)
                .Select(e => e.Specialization.Programmes.RequiredEcts)
                .FirstOrDefaultAsync(ct);
            if (requiredEcts is { } required && required > 0)
            {
                var completedEcts = specSubjects.Where(s => gradedIds.Contains(s.SubjectId)).Sum(s => s.Ects);
                if (completedEcts < required)
                {
                    skipped.Add(new { studentNumber, reason = $"Completion threshold not met ({completedEcts:0.#}/{required:0.#} ECTS graded)." });
                    continue;
                }
            }
            enrolment.StatusId = SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.AwaitingGradesApproval;
            db.Set<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote>().Add(new SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote
            {
                EnrollmentId = enrolment.StudentEnrollmentId,
                StatusId = SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.AwaitingGradesApproval,
                Note = $"{actorLabel} submitted grades via cohort {cohort.CohortNumber}.",
                ByUserId = byUserId,
                CreatedAt = now,
            });
            submitted++;
        }
        if (submitted > 0) await db.SaveChangesAsync(ct);
        return new { submitted, skipped };
    }

    /// <summary>List projection for one partner (or all when partnerId null).</summary>
    public static async Task<List<object>> ListAsync(
        OdinDbContext db, Guid? partnerId, Guid? onlyTeacherId, CancellationToken ct,
        Guid? onlyCohortId = null)
    {
        var query = db.ModuleCohorts.Where(c => c.DeletedAt == null);
        if (partnerId is not null) query = query.Where(c => c.PartnerId == partnerId);
        if (onlyTeacherId is not null) query = query.Where(c => c.TeacherId == onlyTeacherId);
        if (onlyCohortId is not null) query = query.Where(c => c.ModuleCohortId == onlyCohortId);

        var rows = await query
            .OrderByDescending(c => c.StartDate)
            .Select(c => new
            {
                c.ModuleCohortId,
                c.PartnerId,
                PartnerName = db.Partners.Where(p => p.PartnerId == c.PartnerId).Select(p => p.Name).FirstOrDefault(),
                c.CohortNumber,
                c.ProgrammeId,
                ProgrammeName = db.Programmes.Where(p => p.ProgrammeId == c.ProgrammeId).Select(p => p.Name).FirstOrDefault(),
                c.SubjectId,
                ModuleCode = db.Subjects.Where(s => s.SubjectId == c.SubjectId).Select(s => s.Code).FirstOrDefault(),
                ModuleName = db.Subjects.Where(s => s.SubjectId == c.SubjectId).Select(s => s.Name).FirstOrDefault(),
                c.TeacherId,
                TeacherName = db.Teachers.Where(t => t.TeacherId == c.TeacherId).Select(t => t.DisplayName).FirstOrDefault(),
                c.StartDate,
                c.EndDate,
                c.GradingSheetDueOverride,
                c.GradingSheetUploadedDate,
                c.DocQaChecked,
                c.DocQaDate,
                c.GradeQaChecked,
                c.GradeQaDate,
                StudentCount = db.ModuleCohortStudents.Count(s => s.ModuleCohortId == c.ModuleCohortId && s.DeletedAt == null),
            })
            .ToListAsync(ct);

        return rows.Select(c => (object)new
        {
            moduleCohortId = c.ModuleCohortId,
            partnerId = c.PartnerId,
            partnerName = c.PartnerName,
            cohortNumber = c.CohortNumber,
            programmeId = c.ProgrammeId,
            programmeName = c.ProgrammeName,
            subjectId = c.SubjectId,
            moduleCode = c.ModuleCode,
            moduleName = c.ModuleName,
            teacherId = c.TeacherId,
            teacherName = c.TeacherName,
            startDate = c.StartDate,
            endDate = c.EndDate,
            gradingSheetDueDate = c.GradingSheetDueOverride ?? c.EndDate?.AddMonths(1),
            gradingSheetDueIsOverride = c.GradingSheetDueOverride != null,
            gradingSheetUploadedDate = c.GradingSheetUploadedDate,
            docQaChecked = c.DocQaChecked,
            docQaDate = c.DocQaDate,
            gradeQaChecked = c.GradeQaChecked,
            gradeQaDate = c.GradeQaDate,
            studentCount = c.StudentCount,
        }).ToList();
    }

    /// <summary>Full record: list row + configured upload fields with files.</summary>
    public static async Task<object?> DetailAsync(OdinDbContext db, Guid cohortId, CancellationToken ct)
    {
        var row = (await ListAsync(db, null, null, ct, onlyCohortId: cohortId)).FirstOrDefault();
        if (row is null) return null;

        var fields = await db.CohortUploadFields
            .Where(f => f.DeletedAt == null)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);
        var files = await db.CohortUploadFiles
            .Where(f => f.ModuleCohortId == cohortId && f.DeletedAt == null)
            .OrderBy(f => f.UploadedAt)
            .ToListAsync(ct);

        return new
        {
            cohort = row,
            uploadFields = fields.Select(f => new
            {
                id = f.CohortUploadFieldId,
                label = f.Label,
                allowMultiple = f.AllowMultiple,
                isGradingSheet = f.IsGradingSheet,
                files = files
                    .Where(x => x.CohortUploadFieldId == f.CohortUploadFieldId)
                    .Select(x => new { id = x.CohortUploadFileId, fileName = x.FileName, uploadedAt = x.UploadedAt })
                    .ToList(),
            }).ToList(),
        };
    }
}

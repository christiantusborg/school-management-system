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

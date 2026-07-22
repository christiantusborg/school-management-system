using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Rubrics;

/// <summary>
/// Rubric support for the STUDENT-LEVEL grade sheet (admin drawer + partner
/// drawer), mirroring the cohort Grades tab: rubric modules expose their rows
/// with the enrolment's saved per-criterion scores, and a draft save computes
/// the module grade as the weighted total — never taken from the payload.
/// </summary>
public static class RubricGradeLogic
{
    public sealed class RubricEntryDto
    {
        public Guid RowId { get; init; }
        public int? Score { get; init; }
    }

    /// <summary>Per-subject rubric view (name + rows + this enrolment's saved
    /// scores); subjects without a rubric are simply absent.</summary>
    public static async Task<Dictionary<Guid, object>> BuildViewAsync(
        OdinDbContext db, Guid enrollmentId, List<Guid> subjectIds, CancellationToken ct)
    {
        var withRubric = await db.Subjects
            .Where(s => subjectIds.Contains(s.SubjectId) && s.RubricTemplateId != null)
            .Select(s => new { s.SubjectId, TemplateId = s.RubricTemplateId!.Value })
            .ToListAsync(ct);
        if (withRubric.Count == 0) return [];

        var templateIds = withRubric.Select(x => x.TemplateId).Distinct().ToList();
        var names = await db.RubricTemplates
            .Where(t => templateIds.Contains(t.RubricTemplateId))
            .ToDictionaryAsync(t => t.RubricTemplateId, t => t.Name, ct);
        var rows = await db.RubricRows
            .Where(r => templateIds.Contains(r.RubricTemplateId) && r.DeletedAt == null)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(ct);

        var grades = await db.Set<SubjectGrade>()
            .Where(g => g.StudentEnrollmentId == enrollmentId && subjectIds.Contains(g.SubjectId))
            .Select(g => new { g.SubjectId, g.SubjectGradeId })
            .ToListAsync(ct);
        var gradeBySubject = grades.GroupBy(x => x.SubjectId).ToDictionary(g => g.Key, g => g.First().SubjectGradeId);
        var gradeIds = gradeBySubject.Values.ToList();
        var saved = await db.SubjectGradeRubricScores
            .Where(x => gradeIds.Contains(x.SubjectGradeId))
            .ToListAsync(ct);

        var result = new Dictionary<Guid, object>();
        foreach (var s in withRubric)
        {
            gradeBySubject.TryGetValue(s.SubjectId, out var gradeId);
            var scoreByRow = saved
                .Where(x => x.SubjectGradeId == gradeId)
                .GroupBy(x => x.RubricRowId)
                .ToDictionary(g => g.Key, g => g.First().Score);
            result[s.SubjectId] = new
            {
                name = names.GetValueOrDefault(s.TemplateId) ?? "Rubric",
                rows = rows.Where(r => r.RubricTemplateId == s.TemplateId)
                    .Select(r => new
                    {
                        id = r.RubricRowId,
                        section = r.Section,
                        criteria = r.Criteria,
                        maxPercent = r.MaxPercent,
                        score = scoreByRow.TryGetValue(r.RubricRowId, out var sc) ? (int?)sc : null,
                    }).ToList(),
            };
        }
        return result;
    }

    /// <summary>Active rubric rows per rubric-graded subject (id + weight).</summary>
    public static async Task<Dictionary<Guid, List<(Guid RowId, int MaxPercent)>>> LoadRowsAsync(
        OdinDbContext db, IEnumerable<Guid> subjectIds, CancellationToken ct)
    {
        var ids = subjectIds.Distinct().ToList();
        var withRubric = await db.Subjects
            .Where(s => ids.Contains(s.SubjectId) && s.RubricTemplateId != null)
            .Select(s => new { s.SubjectId, TemplateId = s.RubricTemplateId!.Value })
            .ToListAsync(ct);
        if (withRubric.Count == 0) return [];
        var templateIds = withRubric.Select(x => x.TemplateId).Distinct().ToList();
        var rows = await db.RubricRows
            .Where(r => templateIds.Contains(r.RubricTemplateId) && r.DeletedAt == null)
            .Select(r => new { r.RubricTemplateId, r.RubricRowId, r.MaxPercent })
            .ToListAsync(ct);
        return withRubric.ToDictionary(
            s => s.SubjectId,
            s => rows.Where(r => r.RubricTemplateId == s.TemplateId)
                .Select(r => (r.RubricRowId, r.MaxPercent)).ToList());
    }

    /// <summary>Weighted total from a complete set of row scores; error text
    /// when a row is missing or out of range.</summary>
    public static (int Final, string? Error) Compute(
        List<(Guid RowId, int MaxPercent)> rubricRows, List<RubricEntryDto>? provided)
    {
        var byRow = (provided ?? [])
            .Where(rs => rs.Score is not null)
            .GroupBy(rs => rs.RowId)
            .ToDictionary(g => g.Key, g => g.First().Score!.Value);
        if (byRow.Values.Any(v => v is < 0 or > 100))
            return (0, "Rubric scores must be between 0 and 100.");
        if (rubricRows.Any(r => !byRow.ContainsKey(r.RowId)))
            return (0, "All rubric criteria must be scored before the grade can be calculated.");
        var final = (int)Math.Round(
            rubricRows.Sum(r => byRow[r.RowId] * (double)r.MaxPercent) / 100.0,
            MidpointRounding.AwayFromZero);
        return (final, null);
    }

    /// <summary>Upserts the per-row scores behind saved grades. Call after the
    /// SubjectGrade rows exist in the change tracker (ids are client-side
    /// generated), before the caller's final SaveChanges.</summary>
    public static async Task UpsertScoresAsync(
        OdinDbContext db, List<(Guid GradeId, List<(Guid RowId, int Score)> Scores)> pending,
        DateTime now, CancellationToken ct)
    {
        if (pending.Count == 0) return;
        var gradeIds = pending.Select(p => p.GradeId).Distinct().ToList();
        var existing = await db.SubjectGradeRubricScores
            .Where(x => gradeIds.Contains(x.SubjectGradeId))
            .ToListAsync(ct);
        foreach (var (gradeId, scores) in pending)
            foreach (var (rowId, score) in scores)
            {
                var match = existing.FirstOrDefault(x => x.SubjectGradeId == gradeId && x.RubricRowId == rowId);
                if (match is not null) { match.Score = score; match.UpdatedAt = now; }
                else db.SubjectGradeRubricScores.Add(new SubjectGradeRubricScore
                {
                    SubjectGradeId = gradeId,
                    RubricRowId = rowId,
                    Score = score,
                    UpdatedAt = now,
                });
            }
    }
}

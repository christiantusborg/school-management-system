using Odin.Api.Base.Letters;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission Office saves per-subject grades as a DRAFT without advancing the
/// enrolment (mirrors the partner draft-save). The status is unchanged, so the
/// admin can save any subset, keep editing, and download a provisional
/// transcript, then submit ("Program Complete") to move to grade approval.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/grades/draft")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsSaveGradesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/grades/draft", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class GradeEntry
    {
        public Guid SubjectId { get; init; }
        public int? Score { get; init; }
        /// <summary>Rubric-graded modules: one 1-100 score per rubric row;
        /// the module grade is always the weighted total, never Score.</summary>
        public List<School.PartnerAdminApi.Admin.V1.Rubrics.RubricGradeLogic.RubricEntryDto>? Rubric { get; init; }
    }

    public sealed class SaveGradesRequest
    {
        public List<GradeEntry>? Items { get; init; }
        public string? ProjectTitle { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, [FromBody] SaveGradesRequest body,
        OdinDbContext db, LetterReleaseService letterRelease, CancellationToken ct)
    {
        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        // No status gate for the Admission Office: a draft save never changes
        // the enrolment status, so scores may be entered/corrected at ANY
        // stage (even before offer acceptance or after grade approval) and
        // previewed on the provisional transcript. Submission and the
        // partner-side save keep their stricter gates.
        var entries = body.Items ?? new();
        var validSet = (await db.Subjects
            .Where(s => s.SpecializationId == enrolment.SpecializationId && s.DeletedAt == null)
            .Select(s => s.SubjectId)
            .ToListAsync(ct)).ToHashSet();

        // Rubric-graded modules: the final mark is ALWAYS the weighted total
        // of the row scores; simple modules keep the direct 0-100 mark.
        var rubricRowsBySubject = await School.PartnerAdminApi.Admin.V1.Rubrics.RubricGradeLogic.LoadRowsAsync(
            db, entries.Select(x => x.SubjectId), ct);
        var finalScores = new Dictionary<Guid, int>();
        var rubricScores = new Dictionary<Guid, List<(Guid RowId, int Score)>>();
        foreach (var entry in entries)
        {
            if (!validSet.Contains(entry.SubjectId))
                return Results.BadRequest(new { error = $"Subject {entry.SubjectId} doesn't belong to this enrolment." });
            if (rubricRowsBySubject.TryGetValue(entry.SubjectId, out var rubricRows) && rubricRows.Count > 0)
            {
                var (final, error) = School.PartnerAdminApi.Admin.V1.Rubrics.RubricGradeLogic.Compute(rubricRows, entry.Rubric);
                if (error is not null) return Results.BadRequest(new { error });
                finalScores[entry.SubjectId] = final;
                rubricScores[entry.SubjectId] = rubricRows
                    .Select(r => (r.RowId, entry.Rubric!.First(rs => rs.RowId == r.RowId).Score!.Value)).ToList();
            }
            else
            {
                if (entry.Score is null)
                    return Results.BadRequest(new { error = "Score is required." });
                if (entry.Score is < 0 or > 100)
                    return Results.BadRequest(new { error = "Score must be between 0 and 100." });
                finalScores[entry.SubjectId] = entry.Score.Value;
            }
        }

        var existing = await db.Set<SubjectGrade>()
            .Where(g => g.StudentEnrollmentId == enrollmentId)
            .ToListAsync(ct);
        var byId = existing.ToDictionary(g => g.SubjectId);

        var now = DateTime.UtcNow;
        var pendingRubric = new List<(Guid GradeId, List<(Guid RowId, int Score)> Scores)>();
        foreach (var entry in entries)
        {
            if (!byId.TryGetValue(entry.SubjectId, out var row))
            {
                row = new SubjectGrade
                {
                    SubjectGradeId = Guid.NewGuid(),
                    StudentEnrollmentId = enrollmentId,
                    SubjectId = entry.SubjectId,
                };
                db.Set<SubjectGrade>().Add(row);
                byId[entry.SubjectId] = row;
            }
            row.Score = finalScores[entry.SubjectId];
            row.GradedAt = now;
            if (rubricScores.TryGetValue(entry.SubjectId, out var perRow))
                pendingRubric.Add((row.SubjectGradeId, perRow));
        }
        await School.PartnerAdminApi.Admin.V1.Rubrics.RubricGradeLogic.UpsertScoresAsync(db, pendingRubric, now, ct);

        enrolment.ProjectTitle = string.IsNullOrWhiteSpace(body.ProjectTitle) ? null : body.ProjectTitle.Trim();

        // Draft save: no status change, no audit note (silent, repeatable).
        await db.SaveChangesAsync(ct);

        // Grade-bearing letters that were already released must never go
        // stale: re-render each one automatically from the new scores.
        // Best-effort — a render failure never fails the grade save.
        foreach (var (docTypeId, type) in new[]
        {
            (SystemDocumentTypeIds.Transcript, LetterType.Transcript),
            (SystemDocumentTypeIds.PrintableTranscript, LetterType.PrintableTranscript),
            (SystemDocumentTypeIds.Certificate, LetterType.Certificate),
            (SystemDocumentTypeIds.ProvisionalCertificate, LetterType.ProvisionalCertificate),
        })
        {
            var released = await db.StudentDocuments.AnyAsync(d =>
                d.EnrollmentId == enrollmentId && d.DocumentTypeId == docTypeId && d.DeletedAt == null, ct);
            if (released)
            {
                try { await letterRelease.ReleaseAsync(enrollmentId, type, ct); }
                catch { /* keep the grade save even if a re-render fails */ }
            }
        }

        return Results.Ok(new { enrollmentId, saved = entries.Count, statusUnchanged = true });
    }
}

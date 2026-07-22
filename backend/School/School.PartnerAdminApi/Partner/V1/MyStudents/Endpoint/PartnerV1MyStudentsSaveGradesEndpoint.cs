using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Saves per-subject grades as a DRAFT without advancing the enrolment. Unlike
/// the submit ("Program Complete") endpoint, this does not change the status —
/// the partner can save any subset, keep editing, and download a provisional
/// transcript, then click Program Complete to hand the grades to Admission.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/grades/draft")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsSaveGradesEndpoint : IEndpointMarker
{
    private static readonly HashSet<Guid> AllowedStatuses = new()
    {
        EnrollmentStatusIds.AcceptOffer,
        EnrollmentStatusIds.ApplicationApprovedAdmission,
        EnrollmentStatusIds.AcceptAdmission,
        EnrollmentStatusIds.AwaitingGradesSubmit,
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/grades/draft", HandleAsync)
            .RequireAuthorization("PartnerOnly");
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
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.PartnerId == partnerId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        if (!AllowedStatuses.Contains(enrolment.StatusId))
            return Results.BadRequest(new { error = "This enrolment isn't in the grading stage." });

        var entries = body.Items ?? new();

        // Only subjects belonging to this enrolment's specialization, 0..100.
        var validSubjectIds = (await db.Subjects
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
            if (!validSubjectIds.Contains(entry.SubjectId))
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

        // No status change and no activity note: a draft save is silent so
        // repeated saves don't spam the activity log.
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { enrollmentId, saved = entries.Count, statusUnchanged = true });
    }
}

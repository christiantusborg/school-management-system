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
    private static readonly HashSet<Guid> AllowedStatuses = new()
    {
        EnrollmentStatusIds.AcceptOffer,
        EnrollmentStatusIds.ApplicationApprovedAdmission,
        EnrollmentStatusIds.AcceptAdmission,
        EnrollmentStatusIds.AwaitingGradesSubmit,
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/grades/draft", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class GradeEntry
    {
        public Guid SubjectId { get; init; }
        public int Score { get; init; }
    }

    public sealed class SaveGradesRequest
    {
        public List<GradeEntry>? Items { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, [FromBody] SaveGradesRequest body,
        OdinDbContext db, CancellationToken ct)
    {
        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        if (!AllowedStatuses.Contains(enrolment.StatusId))
            return Results.BadRequest(new { error = "This enrolment isn't in the grading stage." });

        var entries = body.Items ?? new();
        var validSet = (await db.Subjects
            .Where(s => s.SpecializationId == enrolment.SpecializationId && s.DeletedAt == null)
            .Select(s => s.SubjectId)
            .ToListAsync(ct)).ToHashSet();

        foreach (var entry in entries)
        {
            if (!validSet.Contains(entry.SubjectId))
                return Results.BadRequest(new { error = $"Subject {entry.SubjectId} doesn't belong to this enrolment." });
            if (entry.Score < 0 || entry.Score > 100)
                return Results.BadRequest(new { error = "Score must be between 0 and 100." });
        }

        var existing = await db.Set<SubjectGrade>()
            .Where(g => g.StudentEnrollmentId == enrollmentId)
            .ToListAsync(ct);
        var byId = existing.ToDictionary(g => g.SubjectId);

        var now = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            if (byId.TryGetValue(entry.SubjectId, out var row))
            {
                row.Score = entry.Score;
                row.GradedAt = now;
            }
            else
            {
                db.Set<SubjectGrade>().Add(new SubjectGrade
                {
                    SubjectGradeId = Guid.NewGuid(),
                    StudentEnrollmentId = enrollmentId,
                    SubjectId = entry.SubjectId,
                    Score = entry.Score,
                    GradedAt = now,
                });
            }
        }

        // Draft save: no status change, no audit note (silent, repeatable).
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { enrollmentId, saved = entries.Count, statusUnchanged = true });
    }
}

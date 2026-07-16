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
        public int Score { get; init; }
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

        foreach (var entry in entries)
        {
            if (!validSubjectIds.Contains(entry.SubjectId))
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

        enrolment.ProjectTitle = string.IsNullOrWhiteSpace(body.ProjectTitle) ? null : body.ProjectTitle.Trim();

        // No status change and no activity note: a draft save is silent so
        // repeated saves don't spam the activity log.
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { enrollmentId, saved = entries.Count, statusUnchanged = true });
    }
}

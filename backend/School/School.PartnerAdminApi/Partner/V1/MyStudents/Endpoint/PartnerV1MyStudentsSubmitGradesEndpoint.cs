using System.Security.Claims;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner commits per-subject grades for an enrolment and hands the
/// application back to Admission. Upserts <see cref="SubjectGrade"/>
/// rows (one per subject) and flips the enrolment's StatusId to
/// <c>AwaitingGradesApproval</c> with an audit note.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/grades")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsSubmitGradesEndpoint : IEndpointMarker
{
    private static readonly HashSet<Guid> AllowedFromStatuses = new()
    {
        EnrollmentStatusIds.AcceptOffer,
        EnrollmentStatusIds.ApplicationApprovedAdmission,
        EnrollmentStatusIds.AcceptAdmission,
        EnrollmentStatusIds.AwaitingGradesSubmit,
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/grades", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class GradeEntry
    {
        public Guid SubjectId { get; init; }
        public int Score { get; init; }
    }

    public sealed class SubmitGradesRequest
    {
        public List<GradeEntry>? Items { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, [FromBody] SubmitGradesRequest body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (callerId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.PartnerId == partnerId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        if (!AllowedFromStatuses.Contains(enrolment.StatusId))
            return Results.BadRequest(new { error = "This enrolment isn't ready for grade submission yet." });

        var entries = body.Items ?? new();
        if (entries.Count == 0)
            return Results.BadRequest(new { error = "No grades supplied." });

        // Validate every subject belongs to this enrolment's specialization.
        var validSubjectIds = await db.Subjects
            .Where(s => s.SpecializationId == enrolment.SpecializationId && s.DeletedAt == null)
            .Select(s => s.SubjectId)
            .ToListAsync(ct);
        var validSet = validSubjectIds.ToHashSet();

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

        enrolment.StatusId = EnrollmentStatusIds.AwaitingGradesApproval;
        db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
        {
            EnrollmentStatusNoteId = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StatusId = EnrollmentStatusIds.AwaitingGradesApproval,
            Note = $"Partner submitted grades for {entries.Count} subject(s). Awaiting Admission approval.",
            ByUserId = ParseUserGuid(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)),
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            enrollmentId,
            statusCode = "AwaitingGradesApproval",
            count = entries.Count,
        });
    }

    private static Guid ParseUserGuid(string? userId) =>
        Guid.TryParse(userId, out var g) ? g : Guid.Empty;
}

using System.Security.Claims;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin sends submitted grades back to the partner with a rejection
/// reason. Refuses unless the enrolment is currently in
/// <c>AwaitingGradesApproval</c>. Flips <c>StatusId</c> to
/// <c>AwaitingGradesSubmit</c> and writes one
/// <see cref="EnrollmentStatusNote"/> carrying the reason — the partner
/// surfaces this in their grade-entry modal so they know what to fix.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/reject-grades")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsRejectGradesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/reject-grades", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class RejectRequest
    {
        public string? Reason { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, [FromBody] RejectRequest body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var reason = (body.Reason ?? string.Empty).Trim();
        if (reason.Length < 10)
            return Results.BadRequest(new { error = "Reason must be at least 10 characters." });

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        if (enrolment.StatusId != EnrollmentStatusIds.AwaitingGradesApproval)
            return Results.BadRequest(new { error = "This enrolment isn't awaiting grade approval." });

        enrolment.StatusId = EnrollmentStatusIds.AwaitingGradesSubmit;
        db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
        {
            EnrollmentStatusNoteId = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StatusId = EnrollmentStatusIds.AwaitingGradesSubmit,
            Note = reason,
            ByUserId = ParseUserGuid(callerId),
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            enrollmentId,
            statusCode = "AwaitingGradesSubmit",
        });
    }

    private static Guid ParseUserGuid(string? userId) =>
        Guid.TryParse(userId, out var g) ? g : Guid.Empty;
}

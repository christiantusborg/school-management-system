using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Student accepts the offer extended by Admission. Refuses unless the
/// enrolment is currently in <c>AcceptOffer</c>. On success: flips to
/// <c>ApplicationApprovedAdmission</c> and writes one
/// <see cref="EnrollmentStatusNote"/>.
/// </summary>
[Route("/v1/student/me/application/{enrollmentId:guid}/accept-offer")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeApplicationAcceptOfferEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/student/me/application/{enrollmentId:guid}/accept-offer", HandleAsync)
            .RequireAuthorization("StudentOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid enrollmentId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.UserId == callerId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == student.StudentId
                && e.DeletedAt == null, ct);
        if (enrollment is null) return Results.NotFound();

        if (enrollment.StatusId != EnrollmentStatusIds.AcceptOffer)
            return Results.BadRequest(new { error = "There is no offer awaiting your acceptance on this application." });

        // Phase 3: with the offer letter released by the partner step, the
        // student's acceptance hands the application to the Admission Office
        // for the next review (Admission Letter is released there).
        var now = DateTime.UtcNow;
        enrollment.StatusId = EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission;

        db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
        {
            EnrollmentStatusNoteId = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StatusId = EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission,
            Note = "Student accepted offer.",
            ByUserId = ParseUserGuid(callerId),
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            enrollmentId,
            statusCode = "ApplicationAwaitingReviewByAdmission",
        });
    }

    private static Guid ParseUserGuid(string? userId) =>
        Guid.TryParse(userId, out var g) ? g : Guid.Empty;
}

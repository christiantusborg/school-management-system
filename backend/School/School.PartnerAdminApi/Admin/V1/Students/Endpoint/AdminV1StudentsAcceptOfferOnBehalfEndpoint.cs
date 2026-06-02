using System.Security.Claims;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission Office accepts the offer on behalf of the student. Same
/// transition as the partner version (<c>AcceptOffer</c> →
/// <c>ApplicationAwaitingReviewByAdmission</c>) but with no partner-
/// ownership gate and the audit note records the admin as the actor.
/// Used when neither the student nor the partner has responded but
/// IBSS needs to push the application forward.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/accept-offer-on-behalf")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsAcceptOfferOnBehalfEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/accept-offer-on-behalf", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrollment is null) return Results.NotFound();

        if (enrollment.StatusId != EnrollmentStatusIds.AcceptOffer)
            return Results.BadRequest(new { error = "This enrolment isn't waiting on offer acceptance." });

        enrollment.StatusId = EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission;
        db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
        {
            EnrollmentStatusNoteId = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StatusId = EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission,
            Note = "Admission Office accepted offer on behalf of student.",
            ByUserId = ParseUserGuid(callerId),
            CreatedAt = DateTime.UtcNow,
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

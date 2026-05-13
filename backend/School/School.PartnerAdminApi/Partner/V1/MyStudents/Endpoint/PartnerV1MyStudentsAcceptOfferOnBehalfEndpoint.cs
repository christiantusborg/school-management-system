using System.Security.Claims;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner-side equivalent of the student's accept-offer flow. Lets the
/// partner click "Accept offer on behalf of student" when the student
/// hasn't responded but the partner has authority to proceed. Same state
/// transition as the student endpoint (<c>AcceptOffer</c> →
/// <c>ApplicationAwaitingReviewByAdmission</c>) but the audit note records
/// that the partner did it, not the student.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/accept-offer-on-behalf")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsAcceptOfferOnBehalfEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/accept-offer-on-behalf", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.PartnerId == partnerId
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
            Note = "Partner accepted offer on behalf of student.",
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

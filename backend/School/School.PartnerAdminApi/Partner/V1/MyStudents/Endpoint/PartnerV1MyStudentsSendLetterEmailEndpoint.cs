using Odin.Api.Base.Letters;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Manually sends (or re-sends) the offer/admission email for one of the
/// partner's own enrolments: the authored email template, tag-filled, with the
/// released PDF attached, to the student plus the template's enabled CC/BCC and
/// any ad-hoc addresses supplied here. Mirrors the admin send action but is
/// scoped to the partner's students and limited to the two emailable letter
/// types. Teachers cannot send.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{type}/send-email")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsSendLetterEmailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{type}/send-email", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class SendBody
    {
        public List<string>? CcAdHoc { get; init; }
        public List<string>? BccAdHoc { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, string type,
        [FromBody] SendBody? body,
        HttpContext httpContext,
        LetterEmailService letterEmail,
        OdinDbContext db, CancellationToken ct)
    {
        var (userId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var callerIsTeacher = await db.Users.AnyAsync(u => u.Id == userId && u.IsTeacher, ct);
        if (callerIsTeacher)
            return Results.Json(new { error = "Teachers cannot send letter emails." }, statusCode: StatusCodes.Status403Forbidden);

        if (!Enum.TryParse<LetterType>(type, ignoreCase: true, out var letterType))
            return Results.BadRequest(new { error = $"Unknown letter type '{type}'." });
        if (letterType is not (LetterType.OfferLetter or LetterType.AdmissionLetter))
            return Results.BadRequest(new { error = "Only the Offer Letter and Admission Letter emails can be sent." });

        var owned = await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId && e.PartnerId == partnerId && e.DeletedAt == null, ct);
        if (!owned) return Results.NotFound();

        // Manual send: the partner's click is the gate, so we don't require the
        // template's auto-send flag to be on — only that a template exists.
        var result = await letterEmail.SendForLetterAsync(
            enrollmentId, letterType, body?.CcAdHoc, body?.BccAdHoc, requireEnabled: false, ct);

        return result.Outcome == LetterEmailOutcome.Sent
            ? Results.Ok(new { sent = true, to = result.To, cc = result.Cc, bcc = result.Bcc })
            : Results.BadRequest(new { sent = false, outcome = result.Outcome.ToString(), error = result.Error });
    }
}

using System.Security.Claims;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Letters;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Manually sends (or re-sends) the offer/admission email for an enrolment:
/// the authored email template, tag-filled, with the released PDF attached, to
/// the student plus the template's enabled CC/BCC and any ad-hoc addresses
/// supplied here. Restricted to Administrator and SuperAdministrator, matching
/// the other outward-facing letter actions.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{type}/send-email")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsSendLetterEmailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{type}/send-email", HandleAsync)
            .RequireAuthorization("AdminOnly");
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
        [FromServices] UserManager<ApplicationUser> userManager,
        LetterEmailService letterEmail,
        OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled)
            return Results.Unauthorized();
        var isAdministrator = await userManager.IsInRoleAsync(caller, AdminLevels.Administrator)
            || await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator);
        if (!isAdministrator)
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        if (!Enum.TryParse<LetterType>(type, ignoreCase: true, out var letterType))
            return Results.BadRequest(new { error = $"Unknown letter type '{type}'." });

        var enrolmentExists = await db.Enrollments
            .AnyAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (!enrolmentExists) return Results.NotFound();

        // Manual send: the admin's click is the gate, so we don't require the
        // template's auto-send flag to be on — only that a template exists.
        var result = await letterEmail.SendForLetterAsync(
            enrollmentId, letterType, body?.CcAdHoc, body?.BccAdHoc, requireEnabled: false, ct);

        return result.Outcome == LetterEmailOutcome.Sent
            ? Results.Ok(new { sent = true, to = result.To, cc = result.Cc, bcc = result.Bcc })
            : Results.BadRequest(new { sent = false, outcome = result.Outcome.ToString(), error = result.Error });
    }
}

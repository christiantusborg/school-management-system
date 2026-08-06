using System.Security.Claims;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Letters;

namespace School.PartnerAdminApi.Admin.V1.Letters;

/// <summary>
/// Preview + manual send of the email accompanying a config-created letter
/// type (the drawer's ✉ Send dialog). Preview composes exactly what would
/// send — programme+partner template, tags resolved, [additional text]
/// placeholder filled — so what you see is what goes out. Only types with
/// "Email letter on release" enabled are emailable; any non-read-only admin
/// level may send, matching the Generate rules.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/dynamic-letters/{definitionId:guid}/email")]
[EndpointTag("Admin.Letters")]
public sealed class AdminV1DynamicLetterEmailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        const string baseRoute = "/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/dynamic-letters/{definitionId:guid}/email";
        app.MapGet(baseRoute + "-preview", PreviewAsync).RequireAuthorization("AdminOnly");
        app.MapPost(baseRoute, SendAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class SendBody
    {
        public List<string>? Cc { get; init; }
        public List<string>? Bcc { get; init; }
        public string? AdditionalText { get; init; }
        /// <summary>Staff-edited subject/body from the dialog; win verbatim
        /// over the template composition when present.</summary>
        public string? Subject { get; init; }
        public string? BodyHtml { get; init; }
    }

    private static async Task<IResult?> GuardAsync(
        HttpContext http, UserManager<ApplicationUser> userManager, OdinDbContext db,
        Guid studentId, Guid enrollmentId, Guid definitionId, CancellationToken ct)
    {
        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled) return Results.Unauthorized();
        var isReadOnly = await userManager.IsInRoleAsync(caller, AdminLevels.Viewer)
            || await userManager.IsInRoleAsync(caller, AdminLevels.Sales);
        if (isReadOnly)
            return Results.Json(new { error = "Read-only access cannot send letter emails." }, statusCode: StatusCodes.Status403Forbidden);
        var owns = await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();
        var emailable = await db.LetterTypeDefinitions
            .Where(d => d.LetterTypeDefinitionId == definitionId && d.DeletedAt == null)
            .Select(d => (bool?)d.EmailOnRelease)
            .FirstOrDefaultAsync(ct);
        if (emailable is null) return Results.NotFound();
        if (emailable is false)
            return Results.BadRequest(new { error = "This letter type has 'Email letter on release' switched off." });
        return null;
    }

    private static async Task<IResult> PreviewAsync(
        Guid studentId, Guid enrollmentId, Guid definitionId,
        [FromQuery] string? additionalText,
        HttpContext http, UserManager<ApplicationUser> userManager,
        OdinDbContext db, LetterEmailService letterEmail, CancellationToken ct)
    {
        if (await GuardAsync(http, userManager, db, studentId, enrollmentId, definitionId, ct) is { } fail) return fail;
        var (composeFail, subject, bodyHtml, to, cc, bcc, isEnabled, isDefault) = await letterEmail.ComposeDynamicAsync(
            enrollmentId, definitionId, adHocCc: null, adHocBcc: null, additionalText, ct);
        if (composeFail is not null)
            return Results.BadRequest(new { error = composeFail.Error ?? composeFail.Outcome.ToString() });
        return Results.Ok(new { subject, bodyHtml, to, cc, bcc, isEnabled, isDefault });
    }

    private static async Task<IResult> SendAsync(
        Guid studentId, Guid enrollmentId, Guid definitionId,
        [FromBody] SendBody body,
        HttpContext http, UserManager<ApplicationUser> userManager,
        OdinDbContext db, LetterEmailService letterEmail, CancellationToken ct)
    {
        if (await GuardAsync(http, userManager, db, studentId, enrollmentId, definitionId, ct) is { } fail) return fail;
        var result = await letterEmail.SendForDynamicLetterAsync(
            enrollmentId, definitionId, body.Cc, body.Bcc, body.AdditionalText, requireEnabled: false, ct,
            overrideSubject: body.Subject, overrideBodyHtml: body.BodyHtml);
        return result.Outcome == LetterEmailOutcome.Sent
            ? Results.Ok(new { sent = true, to = result.To, cc = result.Cc, bcc = result.Bcc })
            : Results.BadRequest(new { error = result.Error ?? result.Outcome.ToString() });
    }
}

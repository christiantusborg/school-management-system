using Microsoft.AspNetCore.Identity;
using Odin.Api.Base.Authentication;
using Odin.Api.Base.Email;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Confirms a student applicant's email from the link in the verification
/// email (userId + token). Marks the account and the primary contact email
/// verified, then re-issues a wizard token so the applicant can resume their
/// application where they left off. Idempotent: the same link keeps working
/// for the token's lifetime so an applicant can return repeatedly.
///
/// Path matches the frontend's existing POST so no client change is needed.
/// </summary>
[Route("/v1/public/student-signup/verify-email")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1VerifyEmailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/public/student-signup/verify-email", HandleAsync).AllowAnonymous();
        return app;
    }

    public sealed class VerifyRequest
    {
        public string? UserId { get; init; }
        public string? Token { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] VerifyRequest body,
        OdinDbContext db,
        UserManager<SharedLibrary.Basics.Opaque.Domains.ApplicationUser> userManager,
        StudentEmailVerificationSender verificationSender,
        WizardSessionService wizard,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.UserId) || string.IsNullOrWhiteSpace(body.Token))
            return Results.BadRequest(new { error = "userId and token are required." });

        // Validate the issued token. A null result means the token is unknown
        // or expired (the issuing cache entry is gone): report 410 so the page
        // shows the "expired" message rather than a generic failure.
        var state = await verificationSender.ValidateAsync(body.UserId, body.Token);
        if (state is null)
            return Results.Json(new { error = "This verification link is invalid or has expired." },
                statusCode: StatusCodes.Status410Gone);

        var user = await userManager.FindByIdAsync(body.UserId);
        if (user is null || user.DeletedAt is not null)
            return Results.NotFound(new { error = "Account not found." });

        // Mark the account + primary contact email verified (mirrors the
        // admin/partner confirm-email endpoints).
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);

        var primaryEmail = await db.UserContactEmails
            .FirstOrDefaultAsync(e => e.UserId == user.Id && e.IsPrimary, ct);
        if (primaryEmail is not null) primaryEmail.IsVerified = true;
        await db.SaveChangesAsync(ct);

        // Re-issue a wizard token bound to (user, student) so the applicant can
        // resume the application from the link.
        var student = await db.Students
            .Where(s => s.UserId == user.Id && s.DeletedAt == null)
            .Select(s => new { s.StudentId })
            .FirstOrDefaultAsync(ct);

        string? wizardToken = student is not null
            ? await wizard.IssueAsync(user.Id, student.StudentId)
            : null;

        return Results.Ok(new { verified = true, wizardToken, redirect = "apply" });
    }
}

using Microsoft.AspNetCore.Identity;
using Odin.Api.Base.Authentication;
using QuVian.SharedLibrary.Basics.Dispatchers;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;
using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Command;
using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Resume an interrupted signup. If the wizard was abandoned halfway (closed
/// tab, power cut, backend redeploy that wiped the cached wizard session) the
/// email already has a user + student row, so /start can't register again.
/// Instead the applicant (or the partner / Admission Office filling it in)
/// proves the ORIGINAL password via the OPAQUE login pipeline, and a fresh
/// wizard token is issued for the existing student — every step already saved
/// loads back into the wizard.
/// </summary>
[Route("/v1/public/draft-signup/resume")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1ResumeEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/public/draft-signup/resume-init", InitAsync).AllowAnonymous();
        app.MapPost("/v1/public/draft-signup/resume-finish", FinishAsync).AllowAnonymous();
        return app;
    }

    public sealed class InitRequest
    {
        public string? Email { get; init; }
        public string? BlindedElement { get; init; }
    }

    public sealed class FinishRequest
    {
        public string? Email { get; init; }
        public string? LoginId { get; init; }
        public string? Signature { get; init; }
    }

    private static async Task<IResult> InitAsync(
        [FromBody] InitRequest body, IDispatcher sender, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.BlindedElement))
            return Results.BadRequest(new { error = "email and blindedElement are required." });

        var result = await sender.SendAsync(new LoginV1InitCommand
        {
            Username = body.Email.Trim(),
            BlindedElement = body.BlindedElement,
            DeviceInfo = "signup-wizard-resume",
        }, ct).ConfigureAwait(false);
        if (!result.TryGetResponseRaw(out var data, out var failure)) return failure!;

        return Results.Ok(new
        {
            loginId = data!.LoginId,
            evaluatedElement = data.EvaluatedElement,
            challenge = data.Challenge,
        });
    }

    private static async Task<IResult> FinishAsync(
        [FromBody] FinishRequest body,
        IDispatcher sender,
        OdinDbContext db,
        UserManager<SharedLibrary.Basics.Opaque.Domains.ApplicationUser> userManager,
        WizardSessionService wizard,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email)
            || string.IsNullOrWhiteSpace(body.LoginId)
            || string.IsNullOrWhiteSpace(body.Signature))
        {
            return Results.BadRequest(new { error = "email, loginId and signature are required." });
        }

        var result = await sender.SendAsync(new LoginV1FinishCommand
        {
            LoginId = body.LoginId,
            Signature = body.Signature,
        }, ct).ConfigureAwait(false);
        if (!result.TryGetResponseRaw(out var data, out var failure)) return failure!;
        if (string.IsNullOrWhiteSpace(data!.Token))
            return Results.BadRequest(new
            {
                error = "This account requires MFA — continue the application by logging in to the student portal instead.",
            });

        var user = await userManager.FindByEmailAsync(body.Email.Trim());
        if (user is null) return Results.BadRequest(new { error = "Account not found." });

        var student = await db.Students
            .Where(s => s.UserId == user.Id && s.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        if (student is null)
            return Results.BadRequest(new { error = "This email belongs to a staff or partner account, not a student application." });

        var wizardToken = await wizard.IssueAsync(user.Id, student.StudentId);
        return Results.Ok(new { wizardToken, wizardStep = student.WizardStep });
    }
}

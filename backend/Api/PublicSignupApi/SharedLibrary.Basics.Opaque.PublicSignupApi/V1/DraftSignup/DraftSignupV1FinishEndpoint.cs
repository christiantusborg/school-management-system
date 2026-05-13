using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Authentication;
using Odin.Api.Base.Email;
using QuVian.SharedLibrary.Basics.Dispatchers;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;
using SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Command;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Step 1b — finish. Wraps the OPAQUE register-finalize pipeline (which
/// creates the ApplicationUser + UserProfile + UserContactEmail + a session
/// token), then layers on the wizard-specific objects:
///   - UserProfile.FirstName/LastName
///   - Student row (PartnerId resolved from the cached partner slug)
///   - Wizard token (WizardSessionService) bound to (UserId, StudentId)
///   - Verification email dispatched out of band.
/// </summary>
[Route("/v1/public/draft-signup/finish")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1FinishEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/public/draft-signup/finish", HandleAsync).AllowAnonymous();
        return app;
    }

    public sealed class FinishRequest
    {
        public Guid? RegistrationId { get; init; }
        public string? ClientPublicKey { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] FinishRequest body,
        IDispatcher sender,
        ITransientStateCache cache,
        OdinDbContext db,
        UserManager<SharedLibrary.Basics.Opaque.Domains.ApplicationUser> userManager,
        WizardSessionService wizard,
        StudentEmailVerificationSender verificationSender,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("DraftSignupV1FinishEndpoint");

        if (body.RegistrationId is null || string.IsNullOrWhiteSpace(body.ClientPublicKey))
            return Results.BadRequest(new { error = "registrationId and clientPublicKey are required." });

        // Pull cached wizard-only fields BEFORE Finalize runs (it removes its own
        // `reg:{id}` cache entry, but our `wizdraft:{id}` is independent).
        var cacheKey = $"wizdraft:{body.RegistrationId}";
        var draftState = await cache.GetAsync<DraftSignupV1StartEndpoint.DraftStartCacheState>(cacheKey);
        if (draftState is null)
            return Results.BadRequest(new { error = "Wizard draft state expired or missing — restart the application." });

        var finalizeCommand = new RegisterFinalizeV1CreateCommand
        {
            RegistrationId = body.RegistrationId.Value,
            ClientPublicKey = body.ClientPublicKey,
        };

        var finalizeResult = await sender.SendAsync(finalizeCommand, ct).ConfigureAwait(false);
        if (!finalizeResult.TryGetResponseRaw(out var finalizeData, out var failure))
            return failure!;

        await cache.RemoveAsync(cacheKey);

        var user = await userManager.FindByIdAsync(finalizeData!.UserId);
        if (user is null)
        {
            logger.LogError("[DraftSignup] Finalize succeeded but user {UserId} not found", finalizeData.UserId);
            return Results.StatusCode(500);
        }

        // Stamp the firstName/lastName on the empty UserProfile the Finalize
        // handler created.
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (profile is not null)
        {
            profile.FirstName = draftState.FirstName;
            profile.LastName = draftState.LastName;
        }

        // Resolve partner + create Student row.
        var partner = await db.Partners
            .Where(p => p.Slug == draftState.PartnerSlug && p.DeletedAt == null)
            .Select(p => new { p.PartnerId })
            .FirstOrDefaultAsync(ct);
        if (partner is null)
        {
            logger.LogError("[DraftSignup] Partner slug '{Slug}' disappeared between /start and /finish", draftState.PartnerSlug);
            return Results.BadRequest(new { error = "Partner not found." });
        }

        // Stamp the user's PartnerId on ApplicationUser too — admin queries use it.
        user.PartnerId = partner.PartnerId;
        await userManager.UpdateAsync(user);

        // Add the Student role so role-gated endpoints recognise them.
        if (!await userManager.IsInRoleAsync(user, "Student"))
            await userManager.AddToRoleAsync(user, "Student");

        var student = new SharedLibrary.Basics.Opaque.Domains.Student
        {
            StudentId = Guid.NewGuid(),
            UserId = user.Id,
            PartnerId = partner.PartnerId,
            StudentNumber = GenerateStudentNumber(),
            WizardStep = 1,
        };
        db.Students.Add(student);
        await db.SaveChangesAsync(ct);

        // Issue wizard token.
        var wizardToken = await wizard.IssueAsync(user.Id, student.StudentId);

        // Dispatch verification email out-of-band — failure is non-fatal so the
        // wizard can still proceed.
        try
        {
            await verificationSender.IssueAndSendAsync(user.Id, user.Email!, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[DraftSignup] Verification email send failed for {UserId}", user.Id);
        }

        return Results.Ok(new { wizardToken });
    }

    private static string GenerateStudentNumber()
    {
        // ST-YYYYMMDD-RAND6, e.g. ST-20260427-A8K3LP. Unique-enough at signup
        // volume; admins can rebrand later.
        var rnd = Guid.NewGuid().ToString("N").ToUpperInvariant().Substring(0, 6);
        return $"ST-{DateTime.UtcNow:yyyyMMdd}-{rnd}";
    }
}

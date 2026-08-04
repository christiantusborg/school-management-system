using QuVian.SharedLibrary.Basics.Dispatchers;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;
using SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Command;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Step 1a — start. Wraps the OPAQUE register-init pipeline and stashes the
/// wizard-specific fields (firstName, lastName, partnerSlug) keyed by
/// registrationId so /finish can apply them after the user record is created.
/// </summary>
[Route("/v1/public/draft-signup/start")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1StartEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/public/draft-signup/start", HandleAsync).AllowAnonymous();
        return app;
    }

    public sealed class StartRequest
    {
        public string? PartnerSlug { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? BlindedElement { get; init; }
        /// <summary>Actor ticket minted by the staff add-student modal; lets
        /// /finish attribute the created student to the staff member.</summary>
        public string? ActorTicket { get; init; }
        /// <summary>Referral link (?ref=&lt;UserGuid&gt;): permanent personal
        /// link of a staff member; signups through it carry their name as
        /// "added by". Validated to a real enabled non-student user.</summary>
        public string? Ref { get; init; }
        /// <summary>CRM lead id when the wizard was opened from "Convert to
        /// student" — /finish links the created student back to the lead.</summary>
        public string? CrmLead { get; init; }
    }

    public sealed record DraftStartCacheState(string PartnerSlug, string FirstName, string LastName, string? ActorUserId = null, string? CrmLeadId = null);

    private static async Task<IResult> HandleAsync(
        [FromBody] StartRequest body,
        IDispatcher sender,
        ITransientStateCache cache,
        OdinDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.PartnerSlug)
            || string.IsNullOrWhiteSpace(body.FirstName)
            || string.IsNullOrWhiteSpace(body.LastName)
            || string.IsNullOrWhiteSpace(body.Email)
            || string.IsNullOrWhiteSpace(body.BlindedElement))
        {
            return Results.BadRequest(new { error = "All of partnerSlug, firstName, lastName, email, blindedElement are required." });
        }

        var partnerExists = await db.Partners
            .AnyAsync(p => p.Slug == body.PartnerSlug && p.DeletedAt == null, ct);
        if (!partnerExists)
            return Results.BadRequest(new { error = "Unknown partner slug." });

        // Email already registered? Don't fail into a dead end — tell the
        // wizard so it can offer to CONTINUE the existing application
        // (password-verified via /resume-init + /resume-finish).
        var normalized = body.Email.Trim().ToUpperInvariant();
        var existingUserId = await db.Users
            .Where(u => u.NormalizedEmail == normalized)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);
        if (existingUserId is not null)
        {
            var isStudent = await db.Students.AnyAsync(s => s.UserId == existingUserId && s.DeletedAt == null, ct);
            if (!isStudent)
                return Results.BadRequest(new { error = "This email belongs to a staff or partner account and cannot be used for a student application." });
            return Results.Ok(new { existingUser = true });
        }

        // Username = email (no separate username for student applicants).
        var initCommand = new RegisterInitV1CreateCommand
        {
            UserId = Guid.NewGuid(), // unused by the handler but required by the type
            Username = body.Email.Trim(),
            Email = body.Email.Trim(),
            BlindedElement = body.BlindedElement,
            CreatedAt = DateTime.UtcNow,
        };

        var initResult = await sender.SendAsync(initCommand, ct).ConfigureAwait(false);
        if (!initResult.TryGetResponseRaw(out var initData, out var failure))
            return failure!;

        // Staff-added signup? Resolve the actor ticket to the staff user id.
        string? actorUserId = null;
        if (!string.IsNullOrWhiteSpace(body.ActorTicket))
            actorUserId = await cache.GetAsync<string>($"wizactor:{body.ActorTicket.Trim()}");
        // Referral link fallback: ?ref=<UserGuid> in the shared signup URL.
        if (actorUserId is null && !string.IsNullOrWhiteSpace(body.Ref) && Guid.TryParse(body.Ref.Trim(), out _))
        {
            var refId = body.Ref.Trim();
            var refOk = await db.Users.AnyAsync(u => u.Id == refId && u.IsEnabled && u.DeletedAt == null, ct)
                && !await db.Students.AnyAsync(st => st.UserId == refId && st.DeletedAt == null, ct);
            if (refOk) actorUserId = refId;
        }

        // Stash wizard-only fields so /finish can copy them onto UserProfile + Student.
        await cache.SetAsync(
            $"wizdraft:{initData!.RegistrationId}",
            new DraftStartCacheState(body.PartnerSlug.Trim(), body.FirstName.Trim(), body.LastName.Trim(), actorUserId,
                Guid.TryParse(body.CrmLead, out var crmLeadGuid) ? crmLeadGuid.ToString() : null),
            TimeSpan.FromMinutes(5));

        return Results.Ok(new
        {
            registrationId = initData.RegistrationId,
            evaluatedElement = Convert.ToBase64String(initData.EvaluatedElement),
        });
    }
}

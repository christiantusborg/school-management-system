using System.Text.RegularExpressions;
// File namespace contains the segment "Partner", which shadows the entity
// type of the same name. Alias the entity type and the child rows so we can
// `new` them directly without fully-qualifying.
using PartnerEntity = SharedLibrary.Basics.Opaque.Domains.Partners.Partner;
using PartnerAddressEntity = SharedLibrary.Basics.Opaque.Domains.Partners.PartnerAddress;
using PartnerContactEmailEntity = SharedLibrary.Basics.Opaque.Domains.Partners.PartnerContactEmail;
using PartnerContactPhoneEntity = SharedLibrary.Basics.Opaque.Domains.Partners.PartnerContactPhone;
using PartnerContractEntity = SharedLibrary.Basics.Opaque.Domains.Partners.PartnerContract;

namespace School.PartnerAdminApi.Partner.V1.Create.Endpoint;

/// <summary>
/// Creates a new partner organisation + its first partner user in one shot.
/// Drives the admin "Create New Partner" wizard. The wizard chains two more
/// requests after this one (POST /users for additional users, POST
/// /programme-access for granting specialization access) — both of those
/// hit their own existing endpoints.
///
/// Persists: <see cref="Partner"/> (Name, Slug, Website, RegistrationNumber,
/// TaxId) + optional <see cref="PartnerAddress"/>, <see cref="PartnerContactEmail"/>,
/// <see cref="PartnerContactPhone"/>, <see cref="PartnerContract"/> child rows,
/// then creates the first user via <see cref="OpaqueUserCreationService"/>.
/// Fields the wizard collects but the entity has no home for
/// (contactPersonName / contactPersonTitle / tier / internalNotes) are
/// silently dropped, matching the existing PATCH endpoint's behaviour.
/// </summary>
[Route("/v1/admin/school/partners")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/school/partners", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class CreateRequest
    {
        public string? Name { get; init; }
        public string? Username { get; init; }
        public string? Email { get; init; }
        // Optional. When blank, server generates a random password (default
        // behaviour). When provided, the wizard's "custom password" field is
        // used verbatim — admin is responsible for password strength.
        public string? Password { get; init; }

        // Wizard fields with no entity home — accepted then ignored.
        public string? ContactPersonName { get; init; }
        public string? ContactPersonTitle { get; init; }
        public string? Tier { get; init; }
        public string? InternalNotes { get; init; }

        public string? ContactPersonEmail { get; init; }
        public string? ContactPersonPhone { get; init; }

        public string? AddressLine1 { get; init; }
        public string? AddressLine2 { get; init; }
        public string? City { get; init; }
        public string? StateRegion { get; init; }
        public string? PostalCode { get; init; }
        public string? Country { get; init; }

        public string? Website { get; init; }
        public string? RegistrationNumber { get; init; }
        public string? TaxId { get; init; }

        public DateTime? ContractStart { get; init; }
        public DateTime? ContractEnd { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateRequest body,
        [FromServices] OdinDbContext db,
        [FromServices] OpaqueUserCreationService creator,
        CancellationToken ct)
    {
        var name = body.Name?.Trim();
        var username = body.Username?.Trim();
        if (string.IsNullOrEmpty(name))     return Results.BadRequest("Partner name is required.");
        if (string.IsNullOrEmpty(username)) return Results.BadRequest("First user's username is required.");

        var slug = await GenerateUniqueSlugAsync(name, db, ct);

        var partner = new PartnerEntity
        {
            PartnerId = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Website = NullIfBlank(body.Website),
            RegistrationNumber = NullIfBlank(body.RegistrationNumber),
            TaxId = NullIfBlank(body.TaxId),
        };
        db.Partners.Add(partner);

        // Address — only insert when at least one field is populated. Uses the
        // first PartnerAddressType by SortOrder (the same fallback the PATCH
        // endpoint uses when no address row exists yet).
        if (HasAnyAddressField(body))
        {
            var addressTypeId = await db.PartnerAddressTypes
                .OrderBy(t => t.SortOrder)
                .Select(t => (Guid?)t.PartnerAddressTypeId)
                .FirstOrDefaultAsync(ct);
            if (addressTypeId is not null)
            {
                db.PartnerAddresses.Add(new PartnerAddressEntity
                {
                    PartnerAddressId = Guid.NewGuid(),
                    PartnerId = partner.PartnerId,
                    PartnerAddressTypeId = addressTypeId.Value,
                    Line1 = NullIfBlank(body.AddressLine1),
                    Line2 = NullIfBlank(body.AddressLine2),
                    City = NullIfBlank(body.City),
                    StateRegion = NullIfBlank(body.StateRegion),
                    PostalCode = NullIfBlank(body.PostalCode),
                    CountryCode = NullIfBlank(body.Country) ?? "XX",
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(body.ContactPersonEmail))
        {
            db.PartnerContactEmails.Add(new PartnerContactEmailEntity
            {
                PartnerContactEmailId = Guid.NewGuid(),
                PartnerId = partner.PartnerId,
                Email = body.ContactPersonEmail.Trim(),
                IsPrimary = true,
            });
        }

        if (!string.IsNullOrWhiteSpace(body.ContactPersonPhone))
        {
            db.PartnerContactPhones.Add(new PartnerContactPhoneEntity
            {
                PartnerContactPhoneId = Guid.NewGuid(),
                PartnerId = partner.PartnerId,
                Phone = body.ContactPersonPhone.Trim(),
                IsPrimary = true,
            });
        }

        if (body.ContractStart is { } start)
        {
            db.PartnerContracts.Add(new PartnerContractEntity
            {
                PartnerContractId = Guid.NewGuid(),
                PartnerId = partner.PartnerId,
                StartDate = DateTime.SpecifyKind(start, DateTimeKind.Utc),
                EndDate = body.ContractEnd is { } end
                    ? DateTime.SpecifyKind(end, DateTimeKind.Utc)
                    : null,
            });
        }

        // Create the first user. The service throws on username collision
        // (same shape as AdminPartnerV1AddUserEndpoint) — convert to 400 so
        // the wizard surfaces the message and the EF transaction rolls back.
        string userName;
        string temporaryPassword;
        try
        {
            var email = NullIfBlank(body.Email) ?? $"{username}@partner.local";
            var (user, password) = await creator.CreateUserAsync(
                username, email, "Partner", partner.PartnerId, ct,
                customPassword: NullIfBlank(body.Password));
            userName = user.UserName ?? username;
            temporaryPassword = password;
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            partnerId = partner.PartnerId,
            username = userName,
            temporaryPassword,
        });
    }

    private static bool HasAnyAddressField(CreateRequest b) =>
        !string.IsNullOrWhiteSpace(b.AddressLine1)
        || !string.IsNullOrWhiteSpace(b.AddressLine2)
        || !string.IsNullOrWhiteSpace(b.City)
        || !string.IsNullOrWhiteSpace(b.StateRegion)
        || !string.IsNullOrWhiteSpace(b.PostalCode)
        || !string.IsNullOrWhiteSpace(b.Country);

    private static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static readonly Regex SlugCleanup = new("[^a-z0-9]+", RegexOptions.Compiled);

    /// <summary>
    /// Produces a URL-safe slug from the partner name and guarantees
    /// uniqueness against existing partners (live or soft-deleted) by
    /// appending -2, -3, … on collision.
    /// </summary>
    private static async Task<string> GenerateUniqueSlugAsync(string name, OdinDbContext db, CancellationToken ct)
    {
        var baseSlug = SlugCleanup.Replace(name.ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "partner";

        var candidate = baseSlug;
        var suffix = 1;
        while (await db.Partners.AnyAsync(p => p.Slug == candidate, ct))
        {
            suffix++;
            candidate = $"{baseSlug}-{suffix}";
        }
        return candidate;
    }
}

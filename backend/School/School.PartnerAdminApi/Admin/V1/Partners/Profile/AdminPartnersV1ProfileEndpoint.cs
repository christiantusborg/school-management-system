using System.Text.RegularExpressions;
using Odin.Api.Base.Authorization;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace School.PartnerAdminApi.Admin.V1.Partners.Profile;

/// <summary>
/// Drives the Admin → Partners → Manage → "Profile" tab.
///
/// GET returns a flat profile DTO synthesised from Partner + the first non-
/// deleted PartnerAddress + the first PartnerContractEmail/Phone + the most
/// recent PartnerContract.
///
/// Frontend expects fields the new domain doesn't store explicitly
/// (`contactPersonName/Title`, `tier`, `internalNotes`). Those round-trip as
/// null until you add them to the domain.
/// </summary>
[Route("/v1/admin/school/partners/{partnerId:guid}")]
[EndpointTag("Admin.Partners.Profile")]
public sealed class AdminPartnersV1ProfileEndpoint : IEndpointMarker
{
    private static readonly Regex SlugPattern = new(
        "^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$",
        RegexOptions.Compiled);

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/school/partners/{partnerId:guid}", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/school/partners/{partnerId:guid}", PatchAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class PatchRequest
    {
        public string? Name { get; init; }
        public string? Slug { get; init; }
        public string? Website { get; init; }
        public string? RegistrationNumber { get; init; }
        public string? TaxId { get; init; }
        public string? ShortCode { get; init; }
        public string? ContactPersonName { get; init; }
        public string? ContactPersonTitle { get; init; }
        public string? ContactPersonEmail { get; init; }
        public string? ContactPersonPhone { get; init; }
        public string? AddressLine1 { get; init; }
        public string? AddressLine2 { get; init; }
        public string? City { get; init; }
        public string? StateRegion { get; init; }
        public string? PostalCode { get; init; }
        public string? Country { get; init; }
        public DateTime? ContractStart { get; init; }
        public DateTime? ContractEnd { get; init; }
        public string? Tier { get; init; }
        public string? InternalNotes { get; init; }
    }

    private static async Task<IResult> GetAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var partner = await db.Partners
            .Where(p => p.PartnerId == partnerId && p.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        if (partner is null) return Results.NotFound();

        var address = await db.PartnerAddresses
            .Where(a => a.PartnerId == partnerId && a.DeletedAt == null)
            .OrderBy(a => a.PartnerAddressId)
            .FirstOrDefaultAsync(ct);
        var contactEmail = await db.PartnerContactEmails
            .Where(e => e.PartnerId == partnerId && e.DeletedAt == null)
            .OrderByDescending(e => e.IsPrimary)
            .ThenBy(e => e.PartnerContactEmailId)
            .FirstOrDefaultAsync(ct);
        var contactPhone = await db.PartnerContactPhones
            .Where(p => p.PartnerId == partnerId && p.DeletedAt == null)
            .OrderByDescending(p => p.IsPrimary)
            .ThenBy(p => p.PartnerContactPhoneId)
            .FirstOrDefaultAsync(ct);
        var contract = await db.PartnerContracts
            .Where(c => c.PartnerId == partnerId && c.DeletedAt == null)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(ct);

        return Results.Ok(new
        {
            partnerId = partner.PartnerId,
            name = partner.Name,
            slug = partner.Slug,
            website = partner.Website,
            registrationNumber = partner.RegistrationNumber,
            taxId = partner.TaxId,
            shortCode = partner.ShortCode,
            contactPersonName = partner.ContactPersonName,
            contactPersonTitle = partner.ContactPersonTitle,
            contactPersonEmail = contactEmail?.Email,
            contactPersonPhone = contactPhone?.Phone,
            // Address.
            addressLine1 = address?.Line1,
            addressLine2 = address?.Line2,
            city = address?.City,
            stateRegion = address?.StateRegion,
            postalCode = address?.PostalCode,
            country = address?.CountryCode,
            // Contract.
            contractStart = contract?.StartDate,
            contractEnd = contract?.EndDate,
            tier = partner.Tier,
            internalNotes = partner.InternalNotes,
        });
    }

    private static async Task<IResult> PatchAsync(
        Guid partnerId, [FromBody] PatchRequest body, OdinDbContext db,
        HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        var partner = await db.Partners
            .FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (partner is null) return Results.NotFound();

        // Slug check (uniqueness + format).
        if (!string.IsNullOrWhiteSpace(body.Slug) && !string.Equals(body.Slug, partner.Slug, StringComparison.OrdinalIgnoreCase)
            && await perms.AccessAsync(http.User, "partner.profile.slug", ct) == AccessLevel.Edit)
        {
            var slug = body.Slug.Trim().ToLowerInvariant();
            if (slug.Length is < 2 or > 40 || !SlugPattern.IsMatch(slug))
                return Results.BadRequest(new { error = "invalid_slug" });
            var taken = await db.Partners.AnyAsync(p =>
                p.Slug == slug && p.DeletedAt == null && p.PartnerId != partnerId, ct);
            if (taken) return Results.BadRequest(new { error = "slug_not_unique" });
            partner.Slug = slug;
        }

        if (body.Name is not null && await perms.AccessAsync(http.User, "partner.profile.name", ct) == AccessLevel.Edit) partner.Name = body.Name.Trim();
        if (body.Website is not null && await perms.AccessAsync(http.User, "partner.profile.website", ct) == AccessLevel.Edit) partner.Website = string.IsNullOrWhiteSpace(body.Website) ? null : body.Website.Trim();
        if (body.RegistrationNumber is not null && await perms.AccessAsync(http.User, "partner.profile.registration_no", ct) == AccessLevel.Edit) partner.RegistrationNumber = string.IsNullOrWhiteSpace(body.RegistrationNumber) ? null : body.RegistrationNumber.Trim();
        if (body.TaxId is not null && await perms.AccessAsync(http.User, "partner.profile.tax_id", ct) == AccessLevel.Edit) partner.TaxId = string.IsNullOrWhiteSpace(body.TaxId) ? null : body.TaxId.Trim();
        if (body.ShortCode is not null && await perms.AccessAsync(http.User, "partner.profile.short_code", ct) == AccessLevel.Edit) partner.ShortCode = string.IsNullOrWhiteSpace(body.ShortCode) ? null : body.ShortCode.Trim();
        if (body.ContactPersonName is not null && await perms.AccessAsync(http.User, "partner.profile.contacts", ct) == AccessLevel.Edit) partner.ContactPersonName = string.IsNullOrWhiteSpace(body.ContactPersonName) ? null : body.ContactPersonName.Trim();
        if (body.ContactPersonTitle is not null && await perms.AccessAsync(http.User, "partner.profile.contacts", ct) == AccessLevel.Edit) partner.ContactPersonTitle = string.IsNullOrWhiteSpace(body.ContactPersonTitle) ? null : body.ContactPersonTitle.Trim();
        if (body.Tier is not null && await perms.AccessAsync(http.User, "partner.profile.tier", ct) == AccessLevel.Edit) partner.Tier = string.IsNullOrWhiteSpace(body.Tier) ? null : body.Tier.Trim();
        if (body.InternalNotes is not null && await perms.AccessAsync(http.User, "partner.profile.internal_notes", ct) == AccessLevel.Edit) partner.InternalNotes = string.IsNullOrWhiteSpace(body.InternalNotes) ? null : body.InternalNotes;

        // Address upsert (single primary address).
        var addressFieldsTouched = body.AddressLine1 is not null || body.AddressLine2 is not null
            || body.City is not null || body.StateRegion is not null || body.PostalCode is not null
            || body.Country is not null;
        if (addressFieldsTouched && await perms.AccessAsync(http.User, "partner.profile.address", ct) == AccessLevel.Edit)
        {
            var address = await db.PartnerAddresses
                .FirstOrDefaultAsync(a => a.PartnerId == partnerId && a.DeletedAt == null, ct);
            if (address is null)
            {
                // PartnerAddress requires a PartnerAddressTypeId; when the
                // lookup table is empty the save used to skip SILENTLY —
                // create a default "Primary" type instead so the address
                // always persists.
                var addressTypeId = await db.PartnerAddressTypes
                    .OrderBy(t => t.SortOrder).Select(t => (Guid?)t.PartnerAddressTypeId).FirstOrDefaultAsync(ct);
                if (addressTypeId is null)
                {
                    var defaultType = new PartnerAddressType { Code = "primary", Name = "Primary", SortOrder = 0 };
                    db.PartnerAddressTypes.Add(defaultType);
                    addressTypeId = defaultType.PartnerAddressTypeId;
                }
                address = new PartnerAddress
                {
                    PartnerAddressId = Guid.NewGuid(),
                    PartnerId = partnerId,
                    PartnerAddressTypeId = addressTypeId.Value,
                    CountryCode = body.Country ?? "XX",
                };
                db.PartnerAddresses.Add(address);
            }
            if (address is not null)
            {
                if (body.AddressLine1 is not null) address.Line1 = body.AddressLine1;
                if (body.AddressLine2 is not null) address.Line2 = body.AddressLine2;
                if (body.City is not null) address.City = body.City;
                if (body.StateRegion is not null) address.StateRegion = body.StateRegion;
                if (body.PostalCode is not null) address.PostalCode = body.PostalCode;
                if (body.Country is not null) address.CountryCode = string.IsNullOrWhiteSpace(body.Country) ? "XX" : body.Country;
            }
        }

        // Contact email upsert.
        if (body.ContactPersonEmail is not null && await perms.AccessAsync(http.User, "partner.profile.contacts", ct) == AccessLevel.Edit)
        {
            var email = await db.PartnerContactEmails
                .FirstOrDefaultAsync(e => e.PartnerId == partnerId && e.IsPrimary && e.DeletedAt == null, ct);
            if (string.IsNullOrWhiteSpace(body.ContactPersonEmail))
            {
                if (email is not null) email.DeletedAt = DateTime.UtcNow;
            }
            else if (email is null)
            {
                db.PartnerContactEmails.Add(new PartnerContactEmail
                {
                    PartnerContactEmailId = Guid.NewGuid(),
                    PartnerId = partnerId,
                    Email = body.ContactPersonEmail.Trim(),
                    IsPrimary = true,
                });
            }
            else
            {
                email.Email = body.ContactPersonEmail.Trim();
            }
        }

        // Contact phone upsert.
        if (body.ContactPersonPhone is not null && await perms.AccessAsync(http.User, "partner.profile.contacts", ct) == AccessLevel.Edit)
        {
            var phone = await db.PartnerContactPhones
                .FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.IsPrimary && p.DeletedAt == null, ct);
            if (string.IsNullOrWhiteSpace(body.ContactPersonPhone))
            {
                if (phone is not null) phone.DeletedAt = DateTime.UtcNow;
            }
            else if (phone is null)
            {
                db.PartnerContactPhones.Add(new PartnerContactPhone
                {
                    PartnerContactPhoneId = Guid.NewGuid(),
                    PartnerId = partnerId,
                    Phone = body.ContactPersonPhone.Trim(),
                    IsPrimary = true,
                });
            }
            else
            {
                phone.Phone = body.ContactPersonPhone.Trim();
            }
        }

        // Contract upsert (single most-recent contract).
        if ((body.ContractStart is not null || body.ContractEnd is not null)
            && await perms.AccessAsync(http.User, "partner.profile.contract", ct) == AccessLevel.Edit)
        {
            var contract = await db.PartnerContracts
                .Where(c => c.PartnerId == partnerId && c.DeletedAt == null)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync(ct);
            if (contract is null)
            {
                contract = new PartnerContract
                {
                    PartnerContractId = Guid.NewGuid(),
                    PartnerId = partnerId,
                    StartDate = body.ContractStart ?? DateTime.UtcNow,
                };
                db.PartnerContracts.Add(contract);
            }
            else if (body.ContractStart is not null)
            {
                contract.StartDate = body.ContractStart.Value;
            }
            if (body.ContractEnd is not null) contract.EndDate = body.ContractEnd;
        }

        // body.Tier and body.InternalNotes are silently dropped — not in the
        // new domain. Add columns to Partner (or a side-table) if you need
        // them persisted.

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerId });
    }
}

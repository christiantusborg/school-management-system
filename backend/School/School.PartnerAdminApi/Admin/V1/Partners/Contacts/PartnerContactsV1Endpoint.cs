using School.PartnerAdminApi.Partner.V1.MyUsers;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace School.PartnerAdminApi.Admin.V1.Partners.Contacts;

/// <summary>
/// Multi-contact book per partner: named contacts typed by role (Owner /
/// Admission / Marketing / Finance / custom) each carrying any number of
/// (method, value) pairs — Email, Phone, WhatsApp, etc.
///
/// Admission manages the full list from the admin partner profile. Partners
/// manage their own list from the partner portal EXCEPT Owner-typed
/// contacts, which only Admission may add, change or remove — the partner
/// PUT silently carries existing Owner contacts over and rejects incoming
/// Owner entries.
/// </summary>
[Route("/v1/admin/school/partners/{partnerId:guid}/contacts")]
[EndpointTag("Admin.Partners.Contacts")]
public sealed class PartnerContactsV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/school/partners/{partnerId:guid}/contacts", AdminGetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/school/partners/{partnerId:guid}/contacts", AdminPutAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/partner/profile/contacts", PartnerGetAsync).RequireAuthorization("PartnerOnly");
        app.MapPut("/v1/partner/profile/contacts", PartnerPutAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/profile/contact-options", PartnerOptionsAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class MethodInput
    {
        public Guid ContactMethodTypeId { get; init; }
        public string? Value { get; init; }
    }

    public sealed class ContactInput
    {
        public Guid PartnerContactTypeId { get; init; }
        public string? Name { get; init; }
        public List<MethodInput>? Methods { get; init; }
    }

    public sealed class PutRequest
    {
        public List<ContactInput>? Contacts { get; init; }
    }

    private static async Task<object> BuildListAsync(OdinDbContext db, Guid partnerId, CancellationToken ct)
    {
        var items = await db.PartnerContacts
            .Where(c => c.PartnerId == partnerId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                partnerContactId = c.PartnerContactId,
                partnerContactTypeId = c.PartnerContactTypeId,
                typeName = c.Type.Name,
                name = c.Name,
                methods = c.Methods.Select(m => new
                {
                    contactMethodTypeId = m.ContactMethodTypeId,
                    methodName = m.MethodType.Name,
                    value = m.Value,
                }).ToList(),
            })
            .ToListAsync(ct);
        return new { items };
    }

    /// <summary>Replace the partner's contact set. When <paramref name="ownerLocked"/>
    /// is true (partner portal) existing Owner contacts are preserved untouched and
    /// incoming Owner-typed entries are rejected.</summary>
    private static async Task<IResult> ReplaceAsync(
        OdinDbContext db, Guid partnerId, PutRequest body, bool ownerLocked, CancellationToken ct)
    {
        var inputs = body.Contacts ?? new();
        var typeIds = inputs.Select(c => c.PartnerContactTypeId).Distinct().ToList();
        var types = await db.PartnerContactTypes
            .Where(t => typeIds.Contains(t.PartnerContactTypeId) && t.DeletedAt == null)
            .ToDictionaryAsync(t => t.PartnerContactTypeId, t => t.Name, ct);
        if (typeIds.Any(id => !types.ContainsKey(id)))
            return Results.BadRequest(new { error = "Unknown contact type." });

        var methodIds = inputs.SelectMany(c => c.Methods ?? new()).Select(m => m.ContactMethodTypeId).Distinct().ToList();
        var enabledMethods = await db.ContactMethodTypes
            .Where(m => methodIds.Contains(m.ContactMethodTypeId) && m.DeletedAt == null)
            .Select(m => m.ContactMethodTypeId)
            .ToListAsync(ct);
        if (methodIds.Any(id => !enabledMethods.Contains(id)))
            return Results.BadRequest(new { error = "One of the contact methods is unknown or disabled." });

        var existing = await db.PartnerContacts
            .Include(c => c.Methods)
            .Where(c => c.PartnerId == partnerId)
            .ToListAsync(ct);

        List<PartnerContact> removable;
        if (ownerLocked)
        {
            var ownerTypeIds = await db.PartnerContactTypes
                .Where(t => t.Name == "Owner")
                .Select(t => t.PartnerContactTypeId)
                .ToListAsync(ct);
            if (inputs.Any(c => ownerTypeIds.Contains(c.PartnerContactTypeId)))
                return Results.BadRequest(new { error = "Owner contacts can only be changed by the Admission Office." });
            removable = existing.Where(c => !ownerTypeIds.Contains(c.PartnerContactTypeId)).ToList();
        }
        else
        {
            removable = existing;
        }

        db.PartnerContacts.RemoveRange(removable);

        var sort = 0;
        foreach (var c in inputs)
        {
            var name = (c.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name)) continue;
            var contact = new PartnerContact
            {
                PartnerId = partnerId,
                PartnerContactTypeId = c.PartnerContactTypeId,
                Name = name,
                SortOrder = sort++,
            };
            db.PartnerContacts.Add(contact);
            foreach (var m in c.Methods ?? new())
            {
                var value = (m.Value ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(value)) continue;
                db.PartnerContactMethods.Add(new PartnerContactMethod
                {
                    PartnerContactId = contact.PartnerContactId,
                    ContactMethodTypeId = m.ContactMethodTypeId,
                    Value = value,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(await BuildListAsync(db, partnerId, ct));
    }

    private static async Task<IResult> AdminGetAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();
        return Results.Ok(await BuildListAsync(db, partnerId, ct));
    }

    private static async Task<IResult> AdminPutAsync(
        Guid partnerId, [FromBody] PutRequest body, OdinDbContext db, CancellationToken ct)
    {
        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();
        return await ReplaceAsync(db, partnerId, body, ownerLocked: false, ct);
    }

    private static async Task<IResult> PartnerGetAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        return Results.Ok(await BuildListAsync(db, partnerId.Value, ct));
    }

    private static async Task<IResult> PartnerPutAsync(
        [FromBody] PutRequest body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        return await ReplaceAsync(db, partnerId.Value, body, ownerLocked: true, ct);
    }

    /// <summary>Enabled contact methods + contact types for the partner
    /// portal's contact editor (Owner listed but flagged locked).</summary>
    private static async Task<IResult> PartnerOptionsAsync(OdinDbContext db, CancellationToken ct)
    {
        var methods = await db.ContactMethodTypes
            .Where(m => m.DeletedAt == null)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
            .Select(m => new { contactMethodTypeId = m.ContactMethodTypeId, name = m.Name })
            .ToListAsync(ct);
        var types = await db.PartnerContactTypes
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
            .Select(t => new { partnerContactTypeId = t.PartnerContactTypeId, name = t.Name, locked = t.Name == "Owner" })
            .ToListAsync(ct);
        return Results.Ok(new { methods, types });
    }
}

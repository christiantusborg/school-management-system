using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace School.ProgrammeApi;

/// <summary>
/// System Config → Contact Methods: two admin-managed lists.
/// `/v1/school/contact-methods` — HOW MGW reaches partners (Email, Phone,
/// WhatsApp, WeChat, …); soft-delete doubles as the disable toggle so only
/// enabled methods are offered when adding a contact method.
/// `/v1/school/contact-types` — WHO the contact is (Owner, Admission,
/// Marketing, Finance, …).
/// Same list/create/update/delete/restore surface as the other simple lists.
/// </summary>
[Route("/v1/school/contact-methods")]
[EndpointTag("School.ContactConfig")]
public sealed class ContactConfigV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/school/contact-methods", MethodsListAsync).RequireAuthorization();
        app.MapGet("/v1/school/contact-methods/options", MethodsOptionsAsync).RequireAuthorization();
        app.MapPost("/v1/school/contact-methods", MethodsCreateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/school/contact-methods/{id:guid}", MethodsUpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/school/contact-methods/{id:guid}", MethodsSoftDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/school/contact-methods/{id:guid}/restore", MethodsRestoreAsync).RequireAuthorization("AdminOnly");

        app.MapGet("/v1/school/contact-types", TypesListAsync).RequireAuthorization();
        app.MapGet("/v1/school/contact-types/options", TypesOptionsAsync).RequireAuthorization();
        app.MapPost("/v1/school/contact-types", TypesCreateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/school/contact-types/{id:guid}", TypesUpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/school/contact-types/{id:guid}", TypesSoftDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/school/contact-types/{id:guid}/restore", TypesRestoreAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class WriteRequest
    {
        public string? Name { get; init; }
        public int? DisplayOrder { get; init; }
    }

    // ── Contact methods ─────────────────────────────────────────────────────
    private static async Task<IResult> MethodsListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.ContactMethodTypes
            .Where(c => includeDeleted || c.DeletedAt == null)
            .OrderBy(c => c.DeletedAt != null).ThenBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { contactMethodTypeId = c.ContactMethodTypeId, name = c.Name, displayOrder = c.DisplayOrder, deletedAt = c.DeletedAt })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> MethodsOptionsAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.ContactMethodTypes
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { contactMethodTypeId = c.ContactMethodTypeId, name = c.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> MethodsCreateAsync(OdinDbContext db, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(new { message = "name is required" });
        var entity = new ContactMethodType { Name = name, DisplayOrder = body.DisplayOrder ?? 0 };
        db.ContactMethodTypes.Add(entity);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { contactMethodTypeId = entity.ContactMethodTypeId });
    }

    private static async Task<IResult> MethodsUpdateAsync(OdinDbContext db, Guid id, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var entity = await db.ContactMethodTypes.FirstOrDefaultAsync(c => c.ContactMethodTypeId == id, ct);
        if (entity is null) return Results.NotFound();
        if (!string.IsNullOrWhiteSpace(body.Name)) entity.Name = body.Name.Trim();
        if (body.DisplayOrder is { } o) entity.DisplayOrder = o;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { contactMethodTypeId = id });
    }

    private static async Task<IResult> MethodsSoftDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.ContactMethodTypes.FirstOrDefaultAsync(c => c.ContactMethodTypeId == id, ct);
        if (entity is null) return Results.NotFound();
        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { contactMethodTypeId = id });
    }

    private static async Task<IResult> MethodsRestoreAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.ContactMethodTypes.FirstOrDefaultAsync(c => c.ContactMethodTypeId == id, ct);
        if (entity is null) return Results.NotFound();
        entity.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { contactMethodTypeId = id });
    }

    // ── Contact types ───────────────────────────────────────────────────────
    private static async Task<IResult> TypesListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.PartnerContactTypes
            .Where(c => includeDeleted || c.DeletedAt == null)
            .OrderBy(c => c.DeletedAt != null).ThenBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { partnerContactTypeId = c.PartnerContactTypeId, name = c.Name, displayOrder = c.DisplayOrder, deletedAt = c.DeletedAt })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> TypesOptionsAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.PartnerContactTypes
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { partnerContactTypeId = c.PartnerContactTypeId, name = c.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> TypesCreateAsync(OdinDbContext db, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(new { message = "name is required" });
        var entity = new PartnerContactType { Name = name, DisplayOrder = body.DisplayOrder ?? 0 };
        db.PartnerContactTypes.Add(entity);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerContactTypeId = entity.PartnerContactTypeId });
    }

    private static async Task<IResult> TypesUpdateAsync(OdinDbContext db, Guid id, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var entity = await db.PartnerContactTypes.FirstOrDefaultAsync(c => c.PartnerContactTypeId == id, ct);
        if (entity is null) return Results.NotFound();
        if (!string.IsNullOrWhiteSpace(body.Name)) entity.Name = body.Name.Trim();
        if (body.DisplayOrder is { } o) entity.DisplayOrder = o;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerContactTypeId = id });
    }

    private static async Task<IResult> TypesSoftDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.PartnerContactTypes.FirstOrDefaultAsync(c => c.PartnerContactTypeId == id, ct);
        if (entity is null) return Results.NotFound();
        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerContactTypeId = id });
    }

    private static async Task<IResult> TypesRestoreAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.PartnerContactTypes.FirstOrDefaultAsync(c => c.PartnerContactTypeId == id, ct);
        if (entity is null) return Results.NotFound();
        entity.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerContactTypeId = id });
    }
}

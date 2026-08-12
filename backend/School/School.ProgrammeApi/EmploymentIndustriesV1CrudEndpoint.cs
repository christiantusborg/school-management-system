using Odin.Api.Base.Authorization;

namespace School.ProgrammeApi;

/// <summary>
/// CRUD for the "current employment industry" list (System Config → Employment
/// Industries), used by the signup wizard's Background step. List (optionally
/// including soft-deleted), Create, Update, SoftDelete, Restore, plus a light
/// Options endpoint.
/// </summary>
[Route("/v1/school/employment-industries")]
[EndpointTag("School.EmploymentIndustries")]
public sealed class EmploymentIndustriesV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/school/employment-industries", ListAsync).RequireAuthorization();
        app.MapGet("/v1/school/employment-industries/options", OptionsAsync).RequireAuthorization();
        app.MapPost("/v1/school/employment-industries", CreateAsync).RequireAuthorization();
        app.MapPut("/v1/school/employment-industries/{id:guid}", UpdateAsync).RequireAuthorization();
        app.MapDelete("/v1/school/employment-industries/{id:guid}", SoftDeleteAsync).RequireAuthorization();
        app.MapPost("/v1/school/employment-industries/{id:guid}/restore", RestoreAsync).RequireAuthorization();
        return app;
    }

    public sealed class WriteRequest
    {
        public string? Name { get; init; }
        public int? DisplayOrder { get; init; }
    }

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.EmploymentIndustries
            .Where(c => includeDeleted || c.DeletedAt == null)
            .OrderBy(c => c.DeletedAt != null)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new { employmentIndustryId = c.EmploymentIndustryId, name = c.Name, displayOrder = c.DisplayOrder, deletedAt = c.DeletedAt })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> OptionsAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.EmploymentIndustries
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { employmentIndustryId = c.EmploymentIndustryId, name = c.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> CreateAsync(OdinDbContext db, [FromBody] WriteRequest body, HttpContext httpContext, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "config.lists", ct) != AccessLevel.Edit) return Results.Forbid();
        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "name is required" });

        var entity = new SharedLibrary.Basics.Opaque.Domains.EmploymentIndustry
        {
            EmploymentIndustryId = Guid.NewGuid(),
            Name = name,
            DisplayOrder = body.DisplayOrder ?? 0,
        };
        db.EmploymentIndustries.Add(entity);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/v1/school/employment-industries/{entity.EmploymentIndustryId}", new { employmentIndustryId = entity.EmploymentIndustryId });
    }

    private static async Task<IResult> UpdateAsync(OdinDbContext db, Guid id, [FromBody] WriteRequest body, HttpContext httpContext, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "config.lists", ct) != AccessLevel.Edit) return Results.Forbid();
        var cur = await db.EmploymentIndustries.FirstOrDefaultAsync(c => c.EmploymentIndustryId == id, ct);
        if (cur is null) return Results.NotFound();
        if (!string.IsNullOrWhiteSpace(body.Name)) cur.Name = body.Name.Trim();
        if (body.DisplayOrder is { } d) cur.DisplayOrder = d;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { employmentIndustryId = id });
    }

    private static async Task<IResult> SoftDeleteAsync(OdinDbContext db, Guid id, HttpContext httpContext, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "config.lists", ct) != AccessLevel.Edit) return Results.Forbid();
        var cur = await db.EmploymentIndustries.FirstOrDefaultAsync(c => c.EmploymentIndustryId == id && c.DeletedAt == null, ct);
        if (cur is null) return Results.NotFound();
        cur.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { employmentIndustryId = id });
    }

    private static async Task<IResult> RestoreAsync(OdinDbContext db, Guid id, HttpContext httpContext, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "config.lists", ct) != AccessLevel.Edit) return Results.Forbid();
        var cur = await db.EmploymentIndustries.FirstOrDefaultAsync(c => c.EmploymentIndustryId == id, ct);
        if (cur is null) return Results.NotFound();
        cur.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { employmentIndustryId = id });
    }
}

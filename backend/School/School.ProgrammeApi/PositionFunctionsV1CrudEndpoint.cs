namespace School.ProgrammeApi;

/// <summary>
/// CRUD for the "current position by function" list (System Config → Position
/// Functions), used by the signup wizard's Background step. List (optionally
/// including soft-deleted), Create, Update, SoftDelete, Restore, plus a light
/// Options endpoint.
/// </summary>
[Route("/v1/school/position-functions")]
[EndpointTag("School.PositionFunctions")]
public sealed class PositionFunctionsV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/school/position-functions", ListAsync).RequireAuthorization();
        app.MapGet("/v1/school/position-functions/options", OptionsAsync).RequireAuthorization();
        app.MapPost("/v1/school/position-functions", CreateAsync).RequireAuthorization();
        app.MapPut("/v1/school/position-functions/{id:guid}", UpdateAsync).RequireAuthorization();
        app.MapDelete("/v1/school/position-functions/{id:guid}", SoftDeleteAsync).RequireAuthorization();
        app.MapPost("/v1/school/position-functions/{id:guid}/restore", RestoreAsync).RequireAuthorization();
        return app;
    }

    public sealed class WriteRequest
    {
        public string? Name { get; init; }
        public int? DisplayOrder { get; init; }
    }

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.PositionFunctions
            .Where(c => includeDeleted || c.DeletedAt == null)
            .OrderBy(c => c.DeletedAt != null)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new { positionFunctionId = c.PositionFunctionId, name = c.Name, displayOrder = c.DisplayOrder, deletedAt = c.DeletedAt })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> OptionsAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.PositionFunctions
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { positionFunctionId = c.PositionFunctionId, name = c.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> CreateAsync(OdinDbContext db, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "name is required" });

        var entity = new SharedLibrary.Basics.Opaque.Domains.PositionFunction
        {
            PositionFunctionId = Guid.NewGuid(),
            Name = name,
            DisplayOrder = body.DisplayOrder ?? 0,
        };
        db.PositionFunctions.Add(entity);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/v1/school/position-functions/{entity.PositionFunctionId}", new { positionFunctionId = entity.PositionFunctionId });
    }

    private static async Task<IResult> UpdateAsync(OdinDbContext db, Guid id, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var cur = await db.PositionFunctions.FirstOrDefaultAsync(c => c.PositionFunctionId == id, ct);
        if (cur is null) return Results.NotFound();
        if (!string.IsNullOrWhiteSpace(body.Name)) cur.Name = body.Name.Trim();
        if (body.DisplayOrder is { } d) cur.DisplayOrder = d;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { positionFunctionId = id });
    }

    private static async Task<IResult> SoftDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var cur = await db.PositionFunctions.FirstOrDefaultAsync(c => c.PositionFunctionId == id && c.DeletedAt == null, ct);
        if (cur is null) return Results.NotFound();
        cur.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { positionFunctionId = id });
    }

    private static async Task<IResult> RestoreAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var cur = await db.PositionFunctions.FirstOrDefaultAsync(c => c.PositionFunctionId == id, ct);
        if (cur is null) return Results.NotFound();
        cur.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { positionFunctionId = id });
    }
}

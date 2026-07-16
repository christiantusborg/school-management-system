using CurrencyEntity = SharedLibrary.Basics.Opaque.Domains.Payments.Currency;

namespace School.ProgrammeApi;

/// <summary>
/// CRUD for payment currencies (System Config → Currencies). List (optionally
/// including soft-deleted), Create, Update, SoftDelete, Restore, plus a light
/// Options endpoint for the payment currency dropdown.
/// </summary>
[Route("/v1/school/currencies")]
[EndpointTag("School.Currencies")]
public sealed class CurrenciesV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/school/currencies", ListAsync).RequireAuthorization();
        app.MapGet("/v1/school/currencies/options", OptionsAsync).RequireAuthorization();
        app.MapPost("/v1/school/currencies", CreateAsync).RequireAuthorization();
        app.MapPut("/v1/school/currencies/{id:guid}", UpdateAsync).RequireAuthorization();
        app.MapDelete("/v1/school/currencies/{id:guid}", SoftDeleteAsync).RequireAuthorization();
        app.MapPost("/v1/school/currencies/{id:guid}/restore", RestoreAsync).RequireAuthorization();
        return app;
    }

    public sealed class WriteRequest
    {
        public string? Code { get; init; }
        public string? Name { get; init; }
        public int? DisplayOrder { get; init; }
    }

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.Currencies
            .Where(c => includeDeleted || c.DeletedAt == null)
            .OrderBy(c => c.DeletedAt != null)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Code)
            .Select(c => new { currencyId = c.CurrencyId, code = c.Code, name = c.Name, displayOrder = c.DisplayOrder, deletedAt = c.DeletedAt })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> OptionsAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.Currencies
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Code)
            .Select(c => new { code = c.Code, name = c.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> CreateAsync(OdinDbContext db, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var code = body.Code?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return Results.BadRequest(new { message = "code is required" });

        var entity = new CurrencyEntity
        {
            CurrencyId = Guid.NewGuid(),
            Code = code,
            Name = string.IsNullOrWhiteSpace(body.Name) ? null : body.Name.Trim(),
            DisplayOrder = body.DisplayOrder ?? 0,
        };
        db.Currencies.Add(entity);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/v1/school/currencies/{entity.CurrencyId}", new { currencyId = entity.CurrencyId });
    }

    private static async Task<IResult> UpdateAsync(OdinDbContext db, Guid id, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var cur = await db.Currencies.FirstOrDefaultAsync(c => c.CurrencyId == id, ct);
        if (cur is null) return Results.NotFound();
        if (!string.IsNullOrWhiteSpace(body.Code)) cur.Code = body.Code.Trim();
        cur.Name = string.IsNullOrWhiteSpace(body.Name) ? null : body.Name.Trim();
        if (body.DisplayOrder is { } d) cur.DisplayOrder = d;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { currencyId = id });
    }

    private static async Task<IResult> SoftDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var cur = await db.Currencies.FirstOrDefaultAsync(c => c.CurrencyId == id && c.DeletedAt == null, ct);
        if (cur is null) return Results.NotFound();
        cur.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { currencyId = id });
    }

    private static async Task<IResult> RestoreAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var cur = await db.Currencies.FirstOrDefaultAsync(c => c.CurrencyId == id, ct);
        if (cur is null) return Results.NotFound();
        cur.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { currencyId = id });
    }
}

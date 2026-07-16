namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Public;

[Route("/v1/public/currencies")]
[EndpointTag("Public.Currencies")]
public sealed class PublicCurrenciesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/currencies", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.Currencies
            .Where(e => e.DeletedAt == null)
            .OrderBy(e => e.DisplayOrder)
            .ThenBy(e => e.Code)
            .Select(e => new { currencyId = e.CurrencyId, code = e.Code, name = e.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items, total = items.Count });
    }
}

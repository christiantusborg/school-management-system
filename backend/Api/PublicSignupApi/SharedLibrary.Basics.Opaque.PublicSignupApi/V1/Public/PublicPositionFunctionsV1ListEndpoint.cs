namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Public;

[Route("/v1/public/position-functions")]
[EndpointTag("Public.PositionFunctions")]
public sealed class PublicPositionFunctionsV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/position-functions", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.PositionFunctions
            .Where(e => e.DeletedAt == null)
            .OrderBy(e => e.DisplayOrder)
            .ThenBy(e => e.Name)
            .Select(e => new { positionFunctionId = e.PositionFunctionId, name = e.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items, total = items.Count });
    }
}

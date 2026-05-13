namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Nationalities.Endpoint;

[Route("/v1/public/nationalities")]
[EndpointTag("Public.Nationalities")]
public sealed class PublicNationalitiesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/nationalities", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(OdinDbContext db, CancellationToken cancellationToken)
    {
        var items = await db.Nationalities
            .Where(n => n.DeletedAt == null)
            .OrderBy(n => n.Name)
            .Select(n => new { nationalityId = n.NationalityId, code = n.Code, name = n.Name })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { items, total = items.Count });
    }
}

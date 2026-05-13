namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Public;

[Route("/v1/public/languages")]
[EndpointTag("Public.Languages")]
public sealed class PublicLanguagesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/languages", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.Languages
            .Where(l => l.DeletedAt == null)
            .OrderBy(l => l.Name)
            .Select(l => new { languageId = l.LanguageId, code = l.Code, name = l.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items, total = items.Count });
    }
}

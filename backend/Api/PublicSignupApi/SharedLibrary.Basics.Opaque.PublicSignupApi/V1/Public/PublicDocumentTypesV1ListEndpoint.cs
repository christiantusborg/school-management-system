namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Public;

[Route("/v1/public/document-types")]
[EndpointTag("Public.DocumentTypes")]
public sealed class PublicDocumentTypesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/document-types", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.DocumentTypes
            .Where(d => d.DeletedAt == null)
            .OrderBy(d => d.Name)
            .Select(d => new { documentTypeId = d.DocumentTypeId, name = d.Name, description = d.Description })
            .ToListAsync(ct);
        return Results.Ok(new { items, total = items.Count });
    }
}

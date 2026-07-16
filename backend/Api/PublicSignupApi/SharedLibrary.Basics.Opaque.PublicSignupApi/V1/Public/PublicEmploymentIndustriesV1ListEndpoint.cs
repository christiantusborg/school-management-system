namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Public;

[Route("/v1/public/employment-industries")]
[EndpointTag("Public.EmploymentIndustries")]
public sealed class PublicEmploymentIndustriesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/employment-industries", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.EmploymentIndustries
            .Where(e => e.DeletedAt == null)
            .OrderBy(e => e.DisplayOrder)
            .ThenBy(e => e.Name)
            .Select(e => new { employmentIndustryId = e.EmploymentIndustryId, name = e.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items, total = items.Count });
    }
}

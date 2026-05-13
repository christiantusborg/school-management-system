namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Public;

[Route("/v1/public/education-levels")]
[EndpointTag("Public.EducationLevels")]
public sealed class PublicEducationLevelsV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/education-levels", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.EducationLevels
            .Where(e => e.DeletedAt == null)
            .OrderBy(e => e.DisplayOrder)
            .ThenBy(e => e.Name)
            .Select(e => new
            {
                educationLevelId = e.EducationLevelId,
                name = e.Name,
                rank = e.Rank,
                displayOrder = e.DisplayOrder,
            })
            .ToListAsync(ct);
        return Results.Ok(new { items, total = items.Count });
    }
}

namespace School.PartnerAdminApi.Partner.V1.DocumentTypes.Endpoint;

/// <summary>
/// Partner-side mirror of the admin document-types list, used to populate
/// the additional-document "kind" dropdown in the partner Documents tab.
/// Same shape, same data, different auth boundary.
/// </summary>
[Route("/v1/partner/document-types")]
[EndpointTag("Partner.DocumentTypes")]
public sealed class PartnerV1DocumentTypesListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/document-types", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.DocumentTypes
            .Where(t => t.DeletedAt == null && !t.IsSystemGenerated)
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                documentTypeId = t.DocumentTypeId,
                name = t.Name,
                description = t.Description,
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }
}

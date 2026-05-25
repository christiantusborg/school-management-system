namespace School.PartnerAdminApi.Admin.V1.DocumentTypes.Endpoint;

/// <summary>
/// Returns the list of non-system document types for the "kind" dropdown
/// in the admin Documents tab when adding an additional document.
/// System-generated types (offer letter, admission letter, transcript,
/// etc.) are excluded — those are produced by the letter pipeline, never
/// uploaded.
/// </summary>
[Route("/v1/admin/document-types")]
[EndpointTag("Admin.DocumentTypes")]
public sealed class AdminV1DocumentTypesListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/document-types", HandleAsync).RequireAuthorization("AdminOnly");
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

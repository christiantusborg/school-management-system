namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Programmes.Endpoint;

/// <summary>
/// Public lookup of which document types a given programme requires.
/// Drives the signup wizard's per-specialization documents step (so the
/// applicant sees the right slot list — passport / degree / language /
/// CV plus any programme-specific extras like a GMAT score later).
/// </summary>
[Route("/v1/public/programmes/{programmeId:guid}/required-documents")]
[EndpointTag("Public.Programmes")]
public sealed class PublicProgrammesV1RequiredDocumentsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/programmes/{programmeId:guid}/required-documents", HandleAsync)
            .AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid programmeId, OdinDbContext db, CancellationToken ct)
    {
        var items = await db.ProgrammeDocumentRequirements
            .Where(r => r.ProgrammeId == programmeId
                && r.DeletedAt == null
                && r.DocumentType.DeletedAt == null)
            .OrderBy(r => r.DocumentType.Name)
            .Select(r => new
            {
                documentTypeId = r.DocumentTypeId,
                name = r.DocumentType.Name,
                description = r.DocumentType.Description,
            })
            .ToListAsync(ct);

        return Results.Ok(new { items });
    }
}

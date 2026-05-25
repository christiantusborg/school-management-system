using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Student-side mirror of the admin/partner document-types list, used to
/// populate the additional-document "kind" dropdown in the student
/// portal.
/// </summary>
[Route("/v1/student/me/document-types")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeDocumentTypesListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/document-types", HandleAsync).RequireAuthorization("StudentOnly");
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

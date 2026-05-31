using School.PartnerAdminApi.SharedStudentEdits;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// PATCH the personal section of any student. No admission gate — IBSS
/// retains edit rights through the entire workflow.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/personal")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsPersonalEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/personal", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromBody] StudentEditService.PersonalDto body,
        OdinDbContext db,
        CancellationToken ct)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        await StudentEditService.UpdatePersonalAsync(db, student, body, "admin", ct);
        return Results.NoContent();
    }
}

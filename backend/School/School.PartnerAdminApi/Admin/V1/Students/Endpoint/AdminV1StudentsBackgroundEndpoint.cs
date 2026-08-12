using Odin.Api.Base.Authorization;
using School.PartnerAdminApi.SharedStudentEdits;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// PATCH the background section of any student. No admission gate.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/background")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsBackgroundEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/background", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromBody] StudentEditService.BackgroundDto body,
        HttpContext httpContext,
        IPermissionService perms,
        OdinDbContext db,
        CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "student.details.background", ct) != AccessLevel.Edit) return Results.Forbid();

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        await StudentEditService.UpdateBackgroundAsync(db, student, body, "admin", ct);
        return Results.NoContent();
    }
}

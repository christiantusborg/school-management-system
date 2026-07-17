using System.Security.Claims;
using Odin.Api.Base.Documents;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Student READ-ONLY view of their own per-module start dates for one
/// enrolment. Set by the Admission Office; default is the commencement date.
/// </summary>
[Route("/v1/student/me/enrollments/{enrollmentId:guid}/module-starts")]
[EndpointTag("Student.Me")]
public sealed class StudentV1MeModuleStartsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/enrollments/{enrollmentId:guid}/module-starts", GetAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetAsync(
        Guid enrollmentId, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var owns = await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.DeletedAt == null
            && db.Students.Any(s => s.StudentId == e.StudentId && s.UserId == callerId && s.DeletedAt == null), ct);
        if (!owns) return Results.NotFound();

        var data = await ModuleStartService.ListAsync(db, enrollmentId, ct);
        return data is null ? Results.NotFound() : Results.Ok(data);
    }
}

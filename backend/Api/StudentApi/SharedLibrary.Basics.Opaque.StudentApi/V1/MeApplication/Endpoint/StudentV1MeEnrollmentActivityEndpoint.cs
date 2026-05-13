using Odin.Api.Base.Services;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Returns the chronological activity feed for one of the calling
/// student's enrolments. Admin actor names are masked as
/// "Admission Office".
/// </summary>
[Route("/v1/student/me/enrollments/{enrollmentId:guid}/activity")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeEnrollmentActivityEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/enrollments/{enrollmentId:guid}/activity", HandleAsync)
            .RequireAuthorization("StudentOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid enrollmentId,
        HttpContext httpContext, OdinDbContext db,
        EnrollmentActivityLogReader reader,
        CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var student = await db.Students
            .Where(s => s.UserId == callerId && s.DeletedAt == null)
            .Select(s => new { s.StudentId })
            .FirstOrDefaultAsync(ct);
        if (student is null) return Results.NotFound();

        var owns = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId
            && e.StudentId == student.StudentId
            && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        var items = await reader.ReadAsync(enrollmentId, maskAdmin: true, ct);
        return Results.Ok(new { items, total = items.Count });
    }
}

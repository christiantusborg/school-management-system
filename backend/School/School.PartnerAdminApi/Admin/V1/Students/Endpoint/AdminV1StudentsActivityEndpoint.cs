using Odin.Api.Base.Services;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Returns the chronological activity feed for one enrolment: every
/// status transition + every document state change, in time order, with
/// actor name + role attached. Admin variant — no admin-name masking.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/activity")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsActivityEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/activity", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        OdinDbContext db,
        EnrollmentActivityLogReader reader,
        CancellationToken ct)
    {
        var owns = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId
            && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        var items = await reader.ReadAsync(enrollmentId, maskAdmin: false, ct);
        return Results.Ok(new { items, total = items.Count });
    }
}

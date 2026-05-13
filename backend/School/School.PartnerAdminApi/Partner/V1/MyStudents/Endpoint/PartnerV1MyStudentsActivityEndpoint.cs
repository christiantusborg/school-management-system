using Odin.Api.Base.Services;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Returns the chronological activity feed for one enrolment owned by the
/// caller's partner. Admin actor names are masked as "Admission Office"
/// to match the existing convention used elsewhere in the partner UI.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/activity")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsActivityEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/activity", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        HttpContext httpContext, OdinDbContext db,
        EnrollmentActivityLogReader reader,
        CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var owns = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId
            && e.PartnerId == partnerId
            && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        var items = await reader.ReadAsync(enrollmentId, maskAdmin: true, ct);
        return Results.Ok(new { items, total = items.Count });
    }
}

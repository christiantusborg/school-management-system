using Odin.Api.Base.Documents;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner READ-ONLY view of their own student's per-module start dates
/// (resolved: explicit override, commencement + N days, or the default
/// commencement date). Editing stays with the Admission Office.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/module-starts")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsModuleStartsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/module-starts", GetAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> GetAsync(
        Guid studentId, Guid enrollmentId, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(http, db, ct);
        if (fail is not null) return fail;
        var owns = await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId && e.PartnerId == partnerId && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        var data = await ModuleStartService.ListAsync(db, enrollmentId, ct);
        return data is null ? Results.NotFound() : Results.Ok(data);
    }
}

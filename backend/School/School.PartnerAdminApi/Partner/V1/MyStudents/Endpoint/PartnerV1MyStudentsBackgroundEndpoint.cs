using School.PartnerAdminApi.Partner.V1.MyUsers;
using School.PartnerAdminApi.SharedStudentEdits;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// PATCH the background section of a student the partner owns. Same
/// admit-gate as the personal endpoint.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/background")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsBackgroundEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/partner/my-students/{studentId:guid}/background", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromBody] StudentEditService.BackgroundDto body,
        HttpContext httpContext,
        OdinDbContext db,
        CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId
                && s.PartnerId == partnerId
                && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        if (await StudentEditService.IsAdmittedAsync(db, studentId, ct))
            return Results.BadRequest(new { error = "Student is admitted; profile is read-only." });

        await StudentEditService.UpdateBackgroundAsync(db, student, body, "partner", ct);
        return Results.NoContent();
    }
}

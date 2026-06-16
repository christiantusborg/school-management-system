using System.Security.Claims;
using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin-only override for an enrolment's commencement (start) date. Past
/// dates are allowed on purpose: IBSS staff routinely backdate a start to a
/// date the programme actually began before the student reached review. The
/// change shifts the expected completion date, so (like the duration
/// override) it is reserved for Administrator and SuperAdministrator levels.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/commencement")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsEnrolmentCommencementEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/commencement", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class CommencementBody
    {
        public DateTime? CommencementDate { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        [FromBody] CommencementBody body,
        HttpContext httpContext,
        [FromServices] UserManager<ApplicationUser> userManager,
        OdinDbContext db, CancellationToken ct)
    {
        // Administrator+ only: changing the start date shifts the completion
        // date, matching the sensitivity of the duration override.
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled)
            return Results.Unauthorized();
        var isAdministrator = await userManager.IsInRoleAsync(caller, AdminLevels.Administrator)
            || await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator);
        if (!isAdministrator)
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        // Normalise to a date with unspecified kind so Npgsql writes it to the
        // `timestamp without time zone` column the same way the review flow does.
        enrolment.CommencementDate = body.CommencementDate is { } d
            ? DateTime.SpecifyKind(d.Date, DateTimeKind.Unspecified)
            : null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { commencementDate = enrolment.CommencementDate });
    }
}

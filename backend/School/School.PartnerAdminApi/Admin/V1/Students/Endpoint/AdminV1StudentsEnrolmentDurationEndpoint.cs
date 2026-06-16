using System.Security.Claims;
using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin-only override for an enrolment's approved duration in months.
/// Unlike the partner review flow, the programme min/max range is NOT
/// enforced here: IBSS admission staff may grant any duration at any
/// point in the lifecycle (including after admission). An out-of-range
/// value is saved and reported back as a warning so the UI can surface
/// it. Restricted to Administrator and SuperAdministrator levels.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/duration")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsEnrolmentDurationEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/duration", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class DurationBody
    {
        public int? ApprovedDurationMonths { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        [FromBody] DurationBody body,
        HttpContext httpContext,
        [FromServices] UserManager<ApplicationUser> userManager,
        OdinDbContext db, CancellationToken ct)
    {
        // Administrator+ only. AdminOnly (any admin level) is not enough:
        // changing an admitted student's duration shifts their completion
        // date, so the action is reserved for the top two levels.
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

        if (body.ApprovedDurationMonths is null)
        {
            enrolment.ApprovedDurationMonths = null;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { approvedDurationMonths = (int?)null, warning = (string?)null });
        }

        var months = body.ApprovedDurationMonths.Value;
        if (months < 1)
            return Results.BadRequest(new { error = "Duration must be at least 1 month." });

        // Out-of-range is allowed for admins; report it as a warning only.
        string? warning = null;
        var range = await db.Specializations
            .Where(s => s.SpecializationId == enrolment.SpecializationId)
            .Select(s => new { s.Programmes.MinDurationMonths, s.Programmes.MaxDurationMonths })
            .FirstOrDefaultAsync(ct);
        if (range is not null
            && range.MaxDurationMonths > 0
            && (months < range.MinDurationMonths || months > range.MaxDurationMonths))
        {
            warning = $"Duration {months} months is outside the programme range ({range.MinDurationMonths}–{range.MaxDurationMonths}).";
        }

        enrolment.ApprovedDurationMonths = months;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { approvedDurationMonths = (int?)months, warning });
    }
}

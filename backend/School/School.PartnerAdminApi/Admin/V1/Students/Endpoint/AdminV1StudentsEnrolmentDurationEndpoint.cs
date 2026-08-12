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
        public int? ApprovedDurationValue { get; init; }
        /// <summary>"Month" or "Day"; defaults to Month when a value is sent.</summary>
        public string? ApprovedDurationUnit { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        [FromBody] DurationBody body,
        HttpContext httpContext,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] IPermissionService perms,
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
        if (!await perms.HasAsync(httpContext.User, AdminPermissions.StudentsEditDuration, ct))
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        if (body.ApprovedDurationValue is null)
        {
            enrolment.ApprovedDurationValue = null;
            enrolment.ApprovedDurationUnit = null;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { approvedDurationValue = (int?)null, approvedDurationUnit = (string?)null, warning = (string?)null });
        }

        var value = body.ApprovedDurationValue.Value;
        var unit = string.Equals(body.ApprovedDurationUnit, SharedLibrary.Basics.Opaque.Domains.DurationDays.UnitDay, StringComparison.OrdinalIgnoreCase)
            ? SharedLibrary.Basics.Opaque.Domains.DurationDays.UnitDay
            : SharedLibrary.Basics.Opaque.Domains.DurationDays.UnitMonth;
        if (value < 1)
            return Results.BadRequest(new { error = "Duration must be at least 1." });

        // Out-of-range is allowed for admins; report it as a warning only.
        // Compare in days (months converted via the commencement calendar).
        string? warning = null;
        var range = await db.Specializations
            .Where(s => s.SpecializationId == enrolment.SpecializationId)
            .Select(s => new { s.Programmes.MinDurationMonths, s.Programmes.MaxDurationMonths, s.Programmes.DurationRangeUnit })
            .FirstOrDefaultAsync(ct);
        if (range is not null && range.MaxDurationMonths > 0)
        {
            var days = SharedLibrary.Basics.Opaque.Domains.DurationDays.ToDays(enrolment.CommencementDate, value, unit)!.Value;
            var minDays = SharedLibrary.Basics.Opaque.Domains.DurationDays.RangeBoundToDays(enrolment.CommencementDate, range.MinDurationMonths, range.DurationRangeUnit);
            var maxDays = SharedLibrary.Basics.Opaque.Domains.DurationDays.RangeBoundToDays(enrolment.CommencementDate, range.MaxDurationMonths, range.DurationRangeUnit);
            if (days < minDays || days > maxDays)
                warning = $"Duration {SharedLibrary.Basics.Opaque.Domains.DurationDays.Display(value, unit)} is outside the programme range ({range.MinDurationMonths}–{range.MaxDurationMonths} {(range.DurationRangeUnit == "Day" ? "days" : "months")}).";
        }

        enrolment.ApprovedDurationValue = value;
        enrolment.ApprovedDurationUnit = unit;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { approvedDurationValue = (int?)value, approvedDurationUnit = (string?)unit, warning });
    }
}

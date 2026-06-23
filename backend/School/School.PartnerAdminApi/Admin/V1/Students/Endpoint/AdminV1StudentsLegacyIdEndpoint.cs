using System.Security.Claims;
using Odin.Api.Base.Authorization;
using School.PartnerAdminApi.SharedStudentEdits;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission-Office-only manual Student ID + "Old student" flag, for students
/// migrated from the old system who already have an externally-assigned ID and
/// skip the offer/admission letter flow. The change is recorded in the normal
/// enrolment Activity log (actor + before/after), not a separate log.
/// Restricted to Administrator and SuperAdministrator (AdminOnly alone is not
/// enough).
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/legacy-id")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsLegacyIdEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/legacy-id", SaveAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class LegacyIdBody
    {
        public string? StudentNumber { get; init; }
        public bool IsLegacyStudent { get; init; }
    }

    private static async Task<IResult> SaveAsync(
        Guid studentId,
        [FromBody] LegacyIdBody body,
        HttpContext http,
        [FromServices] UserManager<ApplicationUser> userManager,
        OdinDbContext db, CancellationToken ct)
    {
        var callerId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled) return Results.Unauthorized();
        var isAdministrator = await userManager.IsInRoleAsync(caller, AdminLevels.Administrator)
            || await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator);
        if (!isAdministrator)
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        var newNumber = body.StudentNumber?.Trim();
        if (string.IsNullOrWhiteSpace(newNumber))
            return Results.BadRequest(new { error = "Student ID is required." });

        var student = await db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        // Student ID is globally unique: reject a collision with a clear message.
        var taken = await db.Students.AnyAsync(s =>
            s.StudentId != studentId && s.StudentNumber == newNumber && s.DeletedAt == null, ct);
        if (taken)
            return Results.Conflict(new { error = $"Student ID \"{newNumber}\" is already in use." });

        var actorUserId = Guid.TryParse(callerId, out var g) ? g : Guid.Empty;
        await StudentEditService.UpdateLegacyIdAsync(db, student, newNumber, body.IsLegacyStudent, actorUserId, ct);

        return Results.Ok(new { studentNumber = student.StudentNumber, isLegacyStudent = student.IsLegacyStudent });
    }
}

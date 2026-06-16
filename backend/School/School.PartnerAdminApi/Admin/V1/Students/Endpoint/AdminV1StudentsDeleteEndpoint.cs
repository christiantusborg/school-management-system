using System.Security.Claims;
using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin removes a wrongly-created applicant. Soft-deletes the student, all
/// of its enrolments, and disables the login account so the record drops out
/// of every list (which all filter <c>DeletedAt == null</c>) and the person
/// can no longer sign in. Nothing is hard-deleted, so the row can be recovered
/// in the database if a deletion was a mistake. Restricted to Administrator and
/// SuperAdministrator: AdminOnly (any admin level) is not enough for a
/// destructive action.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/v1/admin/students/{studentId:guid}", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        HttpContext httpContext,
        [FromServices] UserManager<ApplicationUser> userManager,
        OdinDbContext db, CancellationToken ct)
    {
        // Administrator+ only. Removing an applicant is destructive, so it is
        // reserved for the top two admin levels (same gate as duration edits).
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled)
            return Results.Unauthorized();
        var isAdministrator = await userManager.IsInRoleAsync(caller, AdminLevels.Administrator)
            || await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator);
        if (!isAdministrator)
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        var now = DateTime.UtcNow;
        student.DeletedAt = now;

        var enrolments = await db.Enrollments
            .Where(e => e.StudentId == studentId && e.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var e in enrolments)
            e.DeletedAt = now;

        // Disable the login account so the removed applicant can't sign in.
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == student.UserId, ct);
        if (user is not null)
        {
            user.DeletedAt = now;
            user.IsEnabled = false;
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new { studentId, deleted = true, enrolmentsRemoved = enrolments.Count });
    }
}

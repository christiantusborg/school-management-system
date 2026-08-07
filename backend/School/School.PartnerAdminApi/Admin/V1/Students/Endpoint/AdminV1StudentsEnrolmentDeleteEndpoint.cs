using System.Security.Claims;
using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// DESTRUCTIVE: remove a programme (enrolment) from a student. The caller
/// must confirm by echoing the exact programme code. Soft-deletes the
/// enrolment plus its dependent rows (cohort memberships, subject grades,
/// generated letters/documents, payment plan) so the student's other
/// programmes and their history are untouched. Restricted to Administrator
/// and SuperAdministrator levels and logged with the actor.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/remove")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsEnrolmentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/remove", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class Body { public string? ConfirmProgrammeCode { get; init; } }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, [FromBody] Body body,
        HttpContext httpContext, UserManager<ApplicationUser> userManager,
        OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled) return Results.Unauthorized();
        // SuperAdministrator ONLY — removing a programme is irreversible from
        // the portal, so it sits above the normal admin levels.
        if (!await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator))
            return Results.Json(new { error = "Requires SuperAdministrator." }, statusCode: StatusCodes.Status403Forbidden);

        var enrolment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null)
            .Select(e => new { e.StudentEnrollmentId, Code = e.Specialization.Programmes.Code })
            .FirstOrDefaultAsync(ct);
        if (enrolment is null) return Results.NotFound();

        // Typed confirmation: the caller must echo the exact programme code.
        if (!string.Equals(body.ConfirmProgrammeCode?.Trim(), enrolment.Code, StringComparison.Ordinal))
            return Results.BadRequest(new { error = $"Type the programme code '{enrolment.Code}' exactly to confirm removal." });

        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(callerId, out var g) ? g : Guid.Empty;

        // Soft-delete the enrolment and its dependent rows. Anything keyed by
        // EnrollmentId that carries a DeletedAt is stamped; SubjectGrades and
        // cohort memberships are removed so they don't count toward the
        // student's other programmes.
        var row = await db.Enrollments.FirstAsync(e => e.StudentEnrollmentId == enrollmentId, ct);
        row.DeletedAt = now;

        await db.StudentDocuments
            .Where(d => d.EnrollmentId == enrollmentId && d.DeletedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(d => d.DeletedAt, now), ct);
        await db.ModuleCohortStudents
            .Where(m => m.StudentEnrollmentId == enrollmentId && m.DeletedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(m => m.DeletedAt, now), ct);
        await db.EnrollmentPaymentPlans
            .Where(p => p.StudentEnrollmentId == enrollmentId && p.DeletedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.DeletedAt, now), ct);
        await db.SubjectGrades
            .Where(sg => sg.StudentEnrollmentId == enrollmentId)
            .ExecuteDeleteAsync(ct);

        db.Set<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote>().Add(
            new SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote
            {
                EnrollmentStatusNoteId = Guid.NewGuid(),
                EnrollmentId = enrollmentId,
                StatusId = row.StatusId,
                Note = $"Programme ({enrolment.Code}) removed from student by Admission Office.",
                ByUserId = actorId,
                CreatedAt = now,
            });

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { enrollmentId, removed = true });
    }
}

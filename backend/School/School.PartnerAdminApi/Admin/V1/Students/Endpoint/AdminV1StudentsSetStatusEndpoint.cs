using System.Security.Claims;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Letters;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Lets the Admission Office move an enrolment to any status directly — e.g.
/// re-open a rejected application. Also lists the available statuses for the
/// picker. The change is recorded as an <see cref="EnrollmentStatusNote"/>.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/status")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsSetStatusEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/enrollment-statuses", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/status", SetAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class SetStatusBody
    {
        public Guid StatusId { get; init; }
        public string? Note { get; init; }
    }

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.Set<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus>()
            .Where(s => s.DeletedAt == null)
            .OrderBy(s => s.Level)
            .Select(s => new { statusId = s.EnrollmentStatusId, code = s.Code, name = s.Name, level = s.Level })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> SetAsync(
        Guid studentId, Guid enrollmentId, [FromBody] SetStatusBody body,
        HttpContext httpContext, OdinDbContext db,
        [FromServices] IPermissionService perms,
        LetterReleaseService letterRelease,
        [FromServices] ILogger<AdminV1StudentsSetStatusEndpoint> logger,
        CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "student.enrolment.change_status", ct) != AccessLevel.Edit) return Results.Forbid();

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        var status = await db.Set<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus>()
            .FirstOrDefaultAsync(s => s.EnrollmentStatusId == body.StatusId && s.DeletedAt == null, ct);
        if (status is null) return Results.BadRequest(new { error = "Unknown status." });

        // Status-based access ("act"): the role must hold Edit on both the
        // status it is moving FROM and the one it is moving TO. SuperAdmin
        // bypasses; a status with no explicit row defaults to Edit, so this is
        // a no-op until an admin restricts a status for the role.
        if (await perms.StatusAccessAsync(httpContext.User, enrolment.StatusId, ct) != AccessLevel.Edit
            || await perms.StatusAccessAsync(httpContext.User, status.EnrollmentStatusId, ct) != AccessLevel.Edit)
            return Results.Forbid();

        if (enrolment.StatusId == status.EnrollmentStatusId)
            return Results.Ok(new { statusCode = status.Code, statusName = status.Name });

        enrolment.StatusId = status.EnrollmentStatusId;

        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = string.IsNullOrWhiteSpace(body.Note)
            ? $"Admission Office changed status to \"{status.Name}\"."
            : $"Admission Office changed status to \"{status.Name}\": {body.Note.Trim()}";
        db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
        {
            EnrollmentStatusNoteId = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StatusId = status.EnrollmentStatusId,
            Note = note,
            ByUserId = Guid.TryParse(callerId, out var g) ? g : Guid.Empty,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        // Config-created letter types whose trigger is this status release
        // now (fire-once, best-effort).
        await LetterStatusTriggers.FireAsync(db, letterRelease, logger, enrollmentId, status.EnrollmentStatusId, ct);

        return Results.Ok(new { statusCode = status.Code, statusName = status.Name });
    }
}

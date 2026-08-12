using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission-Office toggle for a student's Moodle (LMS) enabled flag, surfaced
/// on the student detail modal's Moodle tab. Stores a simple per-student
/// boolean; no automatic Moodle provisioning is wired yet.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/moodle")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsMoodleEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/moodle", HandleAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class MoodleBody
    {
        public bool Enabled { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, [FromBody] MoodleBody body, HttpContext httpContext,
        IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "student.moodle.settings", ct) != AccessLevel.Edit) return Results.Forbid();

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        student.MoodleEnabled = body.Enabled;
        student.MoodleUsername = string.IsNullOrWhiteSpace(body.Username) ? null : body.Username.Trim();
        student.MoodlePassword = string.IsNullOrWhiteSpace(body.Password) ? null : body.Password;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            moodleEnabled = student.MoodleEnabled,
            moodleUsername = student.MoodleUsername,
            moodlePassword = student.MoodlePassword,
        });
    }
}

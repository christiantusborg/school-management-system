using System.Security.Claims;
using Odin.Api.Base.Documents;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission-Office view of an enrolment's uploaded assignments: the module
/// tree (Programme → Specialization → Module) with each upload, its required
/// title, download, and the comment chat. Admins may also upload on the
/// student's behalf and join the conversation.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/assignments")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsAssignmentsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        const string baseRoute = "/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/assignments";
        // Reviewers only: ONLY the student uploads assignments (from the
        // student portal). Staff read, download and comment.
        app.MapGet(baseRoute, ListAsync).RequireAuthorization("AdminOnly");
        app.MapGet(baseRoute + "/{assignmentId:guid}/file", FileAsync).RequireAuthorization("AdminOnly");
        app.MapPost(baseRoute + "/{assignmentId:guid}/comments", CommentAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class CommentBody
    {
        public string? Text { get; init; }
    }

    private static async Task<bool> OwnsAsync(OdinDbContext db, Guid studentId, Guid enrollmentId, CancellationToken ct) =>
        await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId && e.DeletedAt == null, ct);

    private static async Task<IResult> ListAsync(
        Guid studentId, Guid enrollmentId, OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        if (!await OwnsAsync(db, studentId, enrollmentId, ct)) return Results.NotFound();
        return Results.Ok(await svc.ListAsync(enrollmentId, ct));
    }

    private static async Task<IResult> FileAsync(
        Guid studentId, Guid enrollmentId, Guid assignmentId,
        OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        if (!await OwnsAsync(db, studentId, enrollmentId, ct)) return Results.NotFound();
        var f = await svc.OpenFileAsync(assignmentId, enrollmentId, ct);
        return f is null ? Results.NotFound() : Results.File(f.Value.Stream, f.Value.MimeType, f.Value.FileName);
    }

    private static async Task<IResult> CommentAsync(
        Guid studentId, Guid enrollmentId, Guid assignmentId, [FromBody] CommentBody body,
        HttpContext http, OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        if (!await OwnsAsync(db, studentId, enrollmentId, ct)) return Results.NotFound();
        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = await db.Users.Where(u => u.Id == callerId).Select(u => u.UserName).FirstOrDefaultAsync(ct);
        var comment = await svc.AddCommentAsync(assignmentId, enrollmentId, "Admission Office", name, callerId, body.Text, ct);
        return comment is null
            ? Results.BadRequest(new { error = "Comment text is required (or the assignment doesn't exist)." })
            : Results.Ok(comment);
    }
}

using System.Security.Claims;
using Odin.Api.Base.Documents;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Student portal assignments: the student uploads their module work
/// (Programme → Specialization → Module) with a required document title,
/// downloads what's there, and chats with teachers/staff in the per-upload
/// comment thread.
/// </summary>
[Route("/v1/student/me/enrollments/{enrollmentId:guid}/assignments")]
[EndpointTag("Student.Me")]
public sealed class StudentV1MeAssignmentsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        const string baseRoute = "/v1/student/me/enrollments/{enrollmentId:guid}/assignments";
        app.MapGet(baseRoute, ListAsync).RequireAuthorization();
        app.MapPost(baseRoute, UploadAsync).RequireAuthorization().DisableAntiforgery();
        app.MapGet(baseRoute + "/{assignmentId:guid}/file", FileAsync).RequireAuthorization();
        app.MapPost(baseRoute + "/{assignmentId:guid}/comments", CommentAsync).RequireAuthorization();
        return app;
    }

    public sealed class CommentBody
    {
        public string? Text { get; init; }
    }

    private static async Task<(bool Ok, string? UserId, string? Name)> OwnsAsync(
        HttpContext http, OdinDbContext db, Guid enrollmentId, CancellationToken ct)
    {
        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return (false, null, null);
        var student = await db.Students
            .Where(s => s.UserId == callerId && s.DeletedAt == null)
            .Select(s => new { s.StudentId })
            .FirstOrDefaultAsync(ct);
        if (student is null) return (false, null, null);
        var owns = await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == student.StudentId && e.DeletedAt == null, ct);
        if (!owns) return (false, null, null);

        var profile = await db.UserProfiles.Where(p => p.UserId == callerId)
            .Select(p => new { p.FirstName, p.LastName }).FirstOrDefaultAsync(ct);
        var name = string.Join(' ', new[] { profile?.FirstName, profile?.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        return (true, callerId, string.IsNullOrWhiteSpace(name) ? null : name);
    }

    private static async Task<IResult> ListAsync(
        Guid enrollmentId, HttpContext http, OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        var (ok, _, _) = await OwnsAsync(http, db, enrollmentId, ct);
        if (!ok) return Results.NotFound();
        return Results.Ok(await svc.ListAsync(enrollmentId, ct));
    }

    private static async Task<IResult> UploadAsync(
        Guid enrollmentId, HttpContext http, OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        var (ok, userId, name) = await OwnsAsync(http, db, enrollmentId, ct);
        if (!ok) return Results.NotFound();
        if (!http.Request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart/form-data required" });

        var form = await http.Request.ReadFormAsync(ct);
        if (!Guid.TryParse(form["subjectId"].ToString(), out var subjectId))
            return Results.BadRequest(new { error = "subjectId is required" });
        var file = form.Files["file"];
        if (file is null) return Results.BadRequest(new { error = "file is required" });

        await using var src = file.OpenReadStream();
        var (error, id) = await svc.UploadAsync(enrollmentId,
            new AssignmentService.UploadInput(subjectId, form["title"].ToString(), src, file.FileName, file.ContentType, file.Length),
            "Student", name, userId, ct);
        return error is null ? Results.Ok(new { assignmentUploadId = id }) : Results.BadRequest(new { error });
    }

    private static async Task<IResult> FileAsync(
        Guid enrollmentId, Guid assignmentId, HttpContext http,
        OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        var (ok, _, _) = await OwnsAsync(http, db, enrollmentId, ct);
        if (!ok) return Results.NotFound();
        var f = await svc.OpenFileAsync(assignmentId, enrollmentId, ct);
        return f is null ? Results.NotFound() : Results.File(f.Value.Stream, f.Value.MimeType, f.Value.FileName);
    }

    private static async Task<IResult> CommentAsync(
        Guid enrollmentId, Guid assignmentId, [FromBody] CommentBody body,
        HttpContext http, OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        var (ok, userId, name) = await OwnsAsync(http, db, enrollmentId, ct);
        if (!ok) return Results.NotFound();
        var comment = await svc.AddCommentAsync(assignmentId, enrollmentId, "Student", name, userId, body.Text, ct);
        return comment is null
            ? Results.BadRequest(new { error = "Comment text is required (or the assignment doesn't exist)." })
            : Results.Ok(comment);
    }
}

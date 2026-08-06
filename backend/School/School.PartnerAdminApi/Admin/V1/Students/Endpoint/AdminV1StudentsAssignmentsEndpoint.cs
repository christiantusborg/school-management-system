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
        app.MapGet(baseRoute, ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost(baseRoute, UploadAsync).RequireAuthorization("AdminOnly").DisableAntiforgery();
        app.MapGet(baseRoute + "/{assignmentId:guid}/file", FileAsync).RequireAuthorization("AdminOnly");
        app.MapPost(baseRoute + "/{assignmentId:guid}/comments", CommentAsync).RequireAuthorization("AdminOnly");
        app.MapPatch(baseRoute + "/{assignmentId:guid}/project-fields", ProjectFieldsAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class CommentBody
    {
        public string? Text { get; init; }
    }

    public sealed class ProjectFieldsBody
    {
        public string? ProjectName { get; init; }
        public string? SupervisorName { get; init; }
        public int? WordCount { get; init; }
        public string? Plagiarism { get; init; }
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

    private static async Task<IResult> UploadAsync(
        Guid studentId, Guid enrollmentId, HttpContext http,
        OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        if (!await OwnsAsync(db, studentId, enrollmentId, ct)) return Results.NotFound();
        if (!http.Request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart/form-data required" });

        var form = await http.Request.ReadFormAsync(ct);
        if (!Guid.TryParse(form["subjectId"].ToString(), out var subjectId))
            return Results.BadRequest(new { error = "subjectId is required" });
        var file = form.Files["file"];
        if (file is null) return Results.BadRequest(new { error = "file is required" });

        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = await db.Users.Where(u => u.Id == callerId).Select(u => u.UserName).FirstOrDefaultAsync(ct);

        await using var src = file.OpenReadStream();
        var (error, id) = await svc.UploadAsync(enrollmentId,
            new AssignmentService.UploadInput(subjectId, form["title"].ToString(), src, file.FileName, file.ContentType, file.Length,
                form["projectName"].ToString(), form["supervisorName"].ToString(),
                int.TryParse(form["wordCount"].ToString(), out var wc) ? wc : null,
                form["plagiarism"].ToString()),
            "Admission Office", name, callerId, ct);
        return error is null ? Results.Ok(new { assignmentUploadId = id }) : Results.BadRequest(new { error });
    }

    private static async Task<IResult> ProjectFieldsAsync(
        Guid studentId, Guid enrollmentId, Guid assignmentId, [FromBody] ProjectFieldsBody body,
        OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        if (!await OwnsAsync(db, studentId, enrollmentId, ct)) return Results.NotFound();
        var ok = await svc.UpdateProjectFieldsAsync(assignmentId, enrollmentId,
            body.ProjectName, body.SupervisorName, body.WordCount, body.Plagiarism, ct);
        return ok ? Results.Ok(new { updated = true }) : Results.NotFound();
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

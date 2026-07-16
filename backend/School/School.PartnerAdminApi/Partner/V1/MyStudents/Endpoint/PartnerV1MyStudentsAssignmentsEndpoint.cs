using System.Security.Claims;
using Odin.Api.Base.Documents;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner view of their own student's uploaded assignments: module tree,
/// downloads and the comment chat. Partner staff may upload on the student's
/// behalf; teacher users (read-only) can still comment — the comments route
/// is whitelisted for them in RolePathGuardMiddleware while the upload route
/// is not.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/assignments")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsAssignmentsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        const string baseRoute = "/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/assignments";
        app.MapGet(baseRoute, ListAsync).RequireAuthorization("PartnerOnly");
        app.MapPost(baseRoute, UploadAsync).RequireAuthorization("PartnerOnly").DisableAntiforgery();
        app.MapGet(baseRoute + "/{assignmentId:guid}/file", FileAsync).RequireAuthorization("PartnerOnly");
        app.MapPost(baseRoute + "/{assignmentId:guid}/comments", CommentAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class CommentBody
    {
        public string? Text { get; init; }
    }

    private static async Task<(bool Ok, string? UserId)> OwnsAsync(
        HttpContext http, OdinDbContext db, Guid studentId, Guid enrollmentId, CancellationToken ct)
    {
        var (userId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(http, db, ct);
        if (fail is not null) return (false, null);
        var owns = await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId && e.PartnerId == partnerId && e.DeletedAt == null, ct);
        return (owns, userId);
    }

    private static async Task<(string Role, string? Name)> AuthorAsync(
        OdinDbContext db, string? userId, CancellationToken ct)
    {
        var u = await db.Users.Where(x => x.Id == userId)
            .Select(x => new { x.UserName, x.IsTeacher }).FirstOrDefaultAsync(ct);
        return (u?.IsTeacher == true ? "Teacher" : "Partner", u?.UserName);
    }

    private static async Task<IResult> ListAsync(
        Guid studentId, Guid enrollmentId, HttpContext http,
        OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        var (ok, _) = await OwnsAsync(http, db, studentId, enrollmentId, ct);
        if (!ok) return Results.NotFound();
        return Results.Ok(await svc.ListAsync(enrollmentId, ct));
    }

    private static async Task<IResult> UploadAsync(
        Guid studentId, Guid enrollmentId, HttpContext http,
        OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        var (ok, userId) = await OwnsAsync(http, db, studentId, enrollmentId, ct);
        if (!ok) return Results.NotFound();
        if (!http.Request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart/form-data required" });

        var form = await http.Request.ReadFormAsync(ct);
        if (!Guid.TryParse(form["subjectId"].ToString(), out var subjectId))
            return Results.BadRequest(new { error = "subjectId is required" });
        var file = form.Files["file"];
        if (file is null) return Results.BadRequest(new { error = "file is required" });

        var (role, name) = await AuthorAsync(db, userId, ct);
        await using var src = file.OpenReadStream();
        var (error, id) = await svc.UploadAsync(enrollmentId,
            new AssignmentService.UploadInput(subjectId, form["title"].ToString(), src, file.FileName, file.ContentType, file.Length),
            role, name, userId, ct);
        return error is null ? Results.Ok(new { assignmentUploadId = id }) : Results.BadRequest(new { error });
    }

    private static async Task<IResult> FileAsync(
        Guid studentId, Guid enrollmentId, Guid assignmentId, HttpContext http,
        OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        var (ok, _) = await OwnsAsync(http, db, studentId, enrollmentId, ct);
        if (!ok) return Results.NotFound();
        var f = await svc.OpenFileAsync(assignmentId, enrollmentId, ct);
        return f is null ? Results.NotFound() : Results.File(f.Value.Stream, f.Value.MimeType, f.Value.FileName);
    }

    private static async Task<IResult> CommentAsync(
        Guid studentId, Guid enrollmentId, Guid assignmentId, [FromBody] CommentBody body,
        HttpContext http, OdinDbContext db, AssignmentService svc, CancellationToken ct)
    {
        var (ok, userId) = await OwnsAsync(http, db, studentId, enrollmentId, ct);
        if (!ok) return Results.NotFound();
        var (role, name) = await AuthorAsync(db, userId, ct);
        var comment = await svc.AddCommentAsync(assignmentId, enrollmentId, role, name, userId, body.Text, ct);
        return comment is null
            ? Results.BadRequest(new { error = "Comment text is required (or the assignment doesn't exist)." })
            : Results.Ok(comment);
    }
}

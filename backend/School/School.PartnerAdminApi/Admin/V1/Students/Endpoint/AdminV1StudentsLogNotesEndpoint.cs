using System.Security.Claims;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission Office side of the immutable student note log (the drawer's Log
/// tab). Notes live on two levels: general on the student, or on one of the
/// student's enrolments (programme/specialization). Content is never edited
/// or deleted; visibility can only be WIDENED, and only on admission-authored
/// notes. Admission always sees every note regardless of flags.
/// </summary>
[Route("/v1/admin/students/{studentId}/log-notes")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsLogNotesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/{studentId:guid}/log-notes", ListAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/students/{studentId:guid}/log-notes", CreateAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/log-notes/{id:guid}/visibility", WidenAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> ListAsync(
        Guid studentId, OdinDbContext db, CancellationToken ct)
    {
        var exists = await db.Students.AnyAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (!exists) return Results.NotFound();

        var notes = await db.StudentLogNotes
            .Where(n => n.StudentId == studentId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                studentLogNoteId = n.StudentLogNoteId,
                enrollmentId = n.StudentEnrollmentId,
                title = n.Title,
                content = n.Content,
                authorRole = n.AuthorRole,
                authorName = n.AuthorName,
                visibleToPartner = n.VisibleToPartner,
                visibleToStudent = n.VisibleToStudent,
                createdAt = n.CreatedAt,
            })
            .ToListAsync(ct);
        return Results.Ok(new { notes });
    }

    public sealed class CreateBody
    {
        public Guid? EnrollmentId { get; init; }
        public string? Title { get; init; }
        public string? Content { get; init; }
        public bool VisibleToPartner { get; init; }
        public bool VisibleToStudent { get; init; }
    }

    private static async Task<IResult> CreateAsync(
        Guid studentId, [FromBody] CreateBody body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var content = body.Content?.Trim();
        if (string.IsNullOrEmpty(content))
            return Results.BadRequest(new { error = "Note text is required." });

        var exists = await db.Students.AnyAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (!exists) return Results.NotFound();

        if (body.EnrollmentId is { } enrId)
        {
            var owns = await db.Enrollments.AnyAsync(e =>
                e.StudentEnrollmentId == enrId && e.StudentId == studentId && e.DeletedAt == null, ct);
            if (!owns) return Results.BadRequest(new { error = "That enrolment doesn't belong to this student." });
        }

        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var authorName = await ResolveNameAsync(db, callerId, ct) ?? "Admission Office";

        var note = new StudentLogNote
        {
            StudentId = studentId,
            StudentEnrollmentId = body.EnrollmentId,
            Title = body.Title?.Trim() ?? "",
            Content = content,
            AuthorRole = "Admission",
            AuthorName = authorName,
            AuthorUserId = callerId,
            VisibleToPartner = body.VisibleToPartner,
            VisibleToStudent = body.VisibleToStudent,
        };
        db.StudentLogNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { studentLogNoteId = note.StudentLogNoteId });
    }

    public sealed class WidenBody
    {
        public bool? VisibleToPartner { get; init; }
        public bool? VisibleToStudent { get; init; }
    }

    private static async Task<IResult> WidenAsync(
        Guid id, [FromBody] WidenBody body, OdinDbContext db, CancellationToken ct)
    {
        var note = await db.StudentLogNotes
            .FirstOrDefaultAsync(n => n.StudentLogNoteId == id, ct);
        if (note is null) return Results.NotFound();

        // Admission has full control both ways (grant AND revoke) on any
        // note, with one invariant: a partner-authored note is always
        // visible to the partner. Partners themselves stay widen-only.
        if (body.VisibleToPartner is { } vp)
        {
            if (!vp && note.AuthorRole == "Partner")
                return Results.BadRequest(new { error = "Partner notes are always visible to the partner." });
            note.VisibleToPartner = vp;
        }
        if (body.VisibleToStudent is { } vs) note.VisibleToStudent = vs;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { visibleToPartner = note.VisibleToPartner, visibleToStudent = note.VisibleToStudent });
    }

    private static async Task<string?> ResolveNameAsync(OdinDbContext db, string? userId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        var p = await db.UserProfiles.Where(x => x.UserId == userId)
            .Select(x => new { x.FirstName, x.LastName }).FirstOrDefaultAsync(ct);
        var name = string.Join(' ', new[] { p?.FirstName, p?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}

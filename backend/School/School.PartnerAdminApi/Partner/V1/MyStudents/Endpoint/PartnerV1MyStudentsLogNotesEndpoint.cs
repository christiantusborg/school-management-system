using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner-staff side of the immutable student note log. Teacher logins are
/// excluded entirely. Partners see admission notes only when admission opened
/// them to the partner, plus every partner-authored note (those are always
/// visible to the Admission Office; that cannot be changed). A partner may
/// widen its OWN notes to the student, never touch admission's visibility,
/// and nothing is ever edited or deleted.
/// </summary>
[Route("/v1/partner/my-students/{studentId}/log-notes")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsLogNotesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students/{studentId:guid}/log-notes", ListAsync)
            .RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my-students/{studentId:guid}/log-notes", CreateAsync)
            .RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/log-notes/{id:guid}/visibility", WidenAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    /// <summary>Resolves the caller's partner and blocks teacher logins
    /// (notes are partner STAFF only, per Admission's decision).</summary>
    private static async Task<(string? UserId, Guid PartnerId, IResult? Fail)> ResolveStaffAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (userId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null)
            return (null, Guid.Empty, fail ?? Results.StatusCode(403));
        var isTeacher = await db.Users.Where(u => u.Id == userId)
            .Select(u => u.IsTeacher).FirstOrDefaultAsync(ct);
        if (isTeacher) return (null, Guid.Empty, Results.StatusCode(403));
        return (userId, partnerId.Value, null);
    }

    private static async Task<Student?> OwnedStudentAsync(
        OdinDbContext db, Guid studentId, Guid partnerId, CancellationToken ct) =>
        await db.Students.FirstOrDefaultAsync(s =>
            s.StudentId == studentId && (s.PartnerId == partnerId || s.Enrollments.Any(pe => pe.PartnerId == partnerId && pe.DeletedAt == null)) && s.DeletedAt == null, ct);

    private static async Task<IResult> ListAsync(
        Guid studentId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await ResolveStaffAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        if (await OwnedStudentAsync(db, studentId, partnerId, ct) is null) return Results.NotFound();

        var notes = await db.StudentLogNotes
            .Where(n => n.StudentId == studentId && (n.AuthorRole == "Partner" || n.VisibleToPartner))
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
        public bool VisibleToStudent { get; init; }
    }

    private static async Task<IResult> CreateAsync(
        Guid studentId, [FromBody] CreateBody body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (userId, partnerId, fail) = await ResolveStaffAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        if (await OwnedStudentAsync(db, studentId, partnerId, ct) is null) return Results.NotFound();

        var content = body.Content?.Trim();
        if (string.IsNullOrEmpty(content))
            return Results.BadRequest(new { error = "Note text is required." });

        if (body.EnrollmentId is { } enrId)
        {
            var owns = await db.Enrollments.AnyAsync(e =>
                e.StudentEnrollmentId == enrId && e.StudentId == studentId && e.DeletedAt == null, ct);
            if (!owns) return Results.BadRequest(new { error = "That enrolment doesn't belong to this student." });
        }

        var profile = await db.UserProfiles.Where(x => x.UserId == userId)
            .Select(x => new { x.FirstName, x.LastName }).FirstOrDefaultAsync(ct);
        var name = string.Join(' ', new[] { profile?.FirstName, profile?.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var note = new StudentLogNote
        {
            StudentId = studentId,
            StudentEnrollmentId = body.EnrollmentId,
            Title = body.Title?.Trim() ?? "",
            Content = content,
            AuthorRole = "Partner",
            AuthorName = string.IsNullOrWhiteSpace(name) ? "Partner" : name,
            AuthorUserId = userId,
            // Partner notes are always visible to the partner (and admission
            // sees everything); the flag is forced and cannot be cleared.
            VisibleToPartner = true,
            VisibleToStudent = body.VisibleToStudent,
        };
        db.StudentLogNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { studentLogNoteId = note.StudentLogNoteId });
    }

    public sealed class WidenBody
    {
        public bool? VisibleToStudent { get; init; }
    }

    private static async Task<IResult> WidenAsync(
        Guid id, [FromBody] WidenBody body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await ResolveStaffAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var note = await (
            from n in db.StudentLogNotes
            join s in db.Students on n.StudentId equals s.StudentId
            where n.StudentLogNoteId == id && (s.PartnerId == partnerId || s.Enrollments.Any(pe => pe.PartnerId == partnerId && pe.DeletedAt == null)) && s.DeletedAt == null
            select n).FirstOrDefaultAsync(ct);
        if (note is null) return Results.NotFound();
        if (note.AuthorRole != "Partner")
            return Results.BadRequest(new { error = "Only the Admission Office can change visibility on its own notes." });

        if (body.VisibleToStudent == true) note.VisibleToStudent = true;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { visibleToPartner = note.VisibleToPartner, visibleToStudent = note.VisibleToStudent });
    }
}

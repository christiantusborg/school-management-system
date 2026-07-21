using System.Security.Claims;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Student portal Notes tab: the notes from the school (Admission Office or
/// the partner) that were explicitly marked visible to the student. Two
/// levels: general on the student, or on one of their programmes. Read-only.
/// </summary>
[Route("/v1/student/me/log-notes")]
[EndpointTag("Student.Me")]
public sealed class StudentV1MeLogNotesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/log-notes", ListAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> ListAsync(
        HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.NotFound();
        var studentId = await db.Students
            .Where(s => s.UserId == callerId && s.DeletedAt == null)
            .Select(s => (Guid?)s.StudentId)
            .FirstOrDefaultAsync(ct);
        if (studentId is null) return Results.NotFound();

        var notes = await (
            from n in db.StudentLogNotes
            where n.StudentId == studentId && n.VisibleToStudent
            join e in db.Enrollments on n.StudentEnrollmentId equals e.StudentEnrollmentId into enrJoin
            from e in enrJoin.DefaultIfEmpty()
            orderby n.CreatedAt descending
            select new
            {
                studentLogNoteId = n.StudentLogNoteId,
                title = n.Title,
                content = n.Content,
                authorRole = n.AuthorRole,
                createdAt = n.CreatedAt,
                programmeName = e != null ? e.Specialization.Programmes.Name : null,
                specializationName = e != null ? e.Specialization.Name : null,
            }).ToListAsync(ct);

        return Results.Ok(new { notes });
    }
}

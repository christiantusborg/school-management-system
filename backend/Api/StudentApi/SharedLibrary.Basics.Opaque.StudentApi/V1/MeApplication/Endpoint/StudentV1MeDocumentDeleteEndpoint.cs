using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Student removes one of their own documents. Refuses if the doc has
/// already been verified by admission (<c>VerifiedByEnrolment</c>) — that
/// belongs to the approved record and shouldn't disappear from a soft-
/// delete on the student's whim. Appends a "Removed by student" audit
/// note before soft-deleting the row.
/// </summary>
[Route("/v1/student/me/documents/{studentDocumentId:guid}")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeDocumentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/v1/student/me/documents/{studentDocumentId:guid}", HandleAsync)
            .RequireAuthorization("StudentOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentDocumentId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.UserId == callerId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        var doc = await db.StudentDocuments
            .FirstOrDefaultAsync(d => d.StudentDocumentId == studentDocumentId
                && d.StudentId == student.StudentId
                && d.DeletedAt == null, ct);
        if (doc is null) return Results.NotFound();

        if (doc.CurrentStatusId == DocumentStatusIds.VerifiedByEnrolment)
            return Results.BadRequest(new { error = "This document has already been verified and cannot be removed." });

        var now = DateTime.UtcNow;
        doc.DeletedAt = now;

        db.StudentDocumentNotes.Add(new StudentDocumentNote
        {
            StudentDocumentNoteId = Guid.NewGuid(),
            StudentDocumentId = doc.StudentDocumentId,
            StatusId = doc.CurrentStatusId,
            ByUserId = callerId,
            Note = "Removed by student.",
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);

        return Results.Ok(new { studentDocumentId });
    }
}

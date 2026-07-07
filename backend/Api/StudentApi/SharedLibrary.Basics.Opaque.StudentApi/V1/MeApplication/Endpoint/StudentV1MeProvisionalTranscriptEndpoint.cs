using Odin.Api.Base.Letters;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Lets a student download a watermarked PROVISIONAL transcript for one of
/// their own enrolments, rendered live from the grades entered so far — before
/// Admission approves and the official transcript is released. Returns 404 when
/// the enrolment isn't the caller's or no published transcript template exists.
/// </summary>
[Route("/v1/student/me/enrollments/{enrollmentId:guid}/transcript/provisional")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeProvisionalTranscriptEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/enrollments/{enrollmentId:guid}/transcript/provisional", HandleAsync)
            .RequireAuthorization("StudentOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid enrollmentId,
        HttpContext httpContext, OdinDbContext db,
        ProvisionalTranscriptService provisional,
        CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var student = await db.Students
            .Where(s => s.UserId == callerId && s.DeletedAt == null)
            .Select(s => new { s.StudentId })
            .FirstOrDefaultAsync(ct);
        if (student is null) return Results.NotFound();

        var owns = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId
            && e.StudentId == student.StudentId
            && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        var pdf = await provisional.RenderAsync(enrollmentId, ct);
        if (pdf is null)
            return Results.NotFound(new { error = "Your transcript isn't available to preview yet." });

        return Results.File(pdf, "application/pdf", $"provisional-transcript-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}

using Odin.Api.Base.Letters;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin download of a watermarked PROVISIONAL transcript for any enrolment,
/// rendered live from the grades saved so far. Available everywhere the real
/// transcript downloads, including before the official transcript is released.
/// Returns 404 when no published transcript template exists for the enrolment's
/// (programme, partner).
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/transcript/provisional")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsProvisionalTranscriptEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/transcript/provisional", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        OdinDbContext db, ProvisionalTranscriptService provisional, CancellationToken ct)
    {
        var exists = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (!exists) return Results.NotFound();

        var pdf = await provisional.RenderAsync(enrollmentId, ct);
        if (pdf is null)
            return Results.NotFound(new { error = "No published transcript template for this programme yet." });

        return Results.File(pdf, "application/pdf", $"provisional-transcript-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}

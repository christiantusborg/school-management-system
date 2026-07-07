using Odin.Api.Base.Letters;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Downloads a watermarked PROVISIONAL transcript for one of the partner's own
/// students, rendered live from the grades saved so far. Lets the partner
/// preview the transcript mid-grading. Returns 404 when the partner doesn't own
/// the enrolment or no published transcript template exists.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/transcript/provisional")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsProvisionalTranscriptEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/transcript/provisional", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        HttpContext httpContext, OdinDbContext db,
        ProvisionalTranscriptService provisional,
        CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var owned = await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId && e.PartnerId == partnerId && e.DeletedAt == null, ct);
        if (!owned) return Results.NotFound();

        var pdf = await provisional.RenderAsync(enrollmentId, ct);
        if (pdf is null)
            return Results.NotFound(new { error = "No published transcript template for this programme yet." });

        return Results.File(pdf, "application/pdf", $"provisional-transcript-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}

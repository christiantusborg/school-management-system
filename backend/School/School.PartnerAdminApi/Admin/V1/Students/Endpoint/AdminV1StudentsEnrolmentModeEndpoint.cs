namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission-Office edit of an enrolment's mode of study (Blended Learning,
/// Online, On-campus, …), from the student-detail Enrolment section — same
/// pattern as the teaching-language override. Any admin level may set it.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/mode-of-study")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsEnrolmentModeEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/mode-of-study", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class ModeBody
    {
        public int ModeOfStudyId { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        [FromBody] ModeBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        var mode = await db.ModesOfStudy
            .Where(m => m.ModeOfStudyId == body.ModeOfStudyId && m.DeletedAt == null)
            .Select(m => new { m.ModeOfStudyId, m.Name })
            .FirstOrDefaultAsync(ct);
        if (mode is null)
            return Results.BadRequest(new { error = "Unknown mode of study." });

        enrolment.ModeOfStudyId = mode.ModeOfStudyId;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { modeOfStudyId = mode.ModeOfStudyId, modeOfStudyName = mode.Name });
    }
}

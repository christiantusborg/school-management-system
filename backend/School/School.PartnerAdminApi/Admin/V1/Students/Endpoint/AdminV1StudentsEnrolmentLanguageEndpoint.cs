namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin override of an enrolment's teaching/instruction language, editable
/// from the student-detail Enrolment section (the same place as commencement
/// and duration). Blank clears the override so the specialization's language
/// applies again. The value flows into every letter via the [instruction
/// language] tag. Any admin level may set it (low-risk, unlike duration).
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/teaching-language")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsEnrolmentLanguageEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/teaching-language", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class LanguageBody
    {
        public string? InstructionLanguageOverride { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        [FromBody] LanguageBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        var lang = body.InstructionLanguageOverride?.Trim();
        enrolment.InstructionLanguageOverride = string.IsNullOrWhiteSpace(lang) ? null : lang;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { instructionLanguageOverride = enrolment.InstructionLanguageOverride });
    }
}

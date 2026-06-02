namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin-only override for an enrolment's approved duration in months.
/// Same validation as the partner review flow (must satisfy the
/// programme's min/max range) but available at any point in the
/// lifecycle so IBSS can correct a partner's mistake without forcing a
/// reject-and-resubmit cycle.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/duration")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsEnrolmentDurationEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/duration", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class DurationBody
    {
        public int? ApprovedDurationMonths { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        [FromBody] DurationBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        if (body.ApprovedDurationMonths is null)
        {
            enrolment.ApprovedDurationMonths = null;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }

        var months = body.ApprovedDurationMonths.Value;
        var range = await db.Specializations
            .Where(s => s.SpecializationId == enrolment.SpecializationId)
            .Select(s => new { s.Programmes.MinDurationMonths, s.Programmes.MaxDurationMonths })
            .FirstOrDefaultAsync(ct);
        if (range is not null
            && range.MaxDurationMonths > 0
            && (months < range.MinDurationMonths || months > range.MaxDurationMonths))
        {
            return Results.BadRequest(new
            {
                error = $"Duration {months} months is outside the programme range ({range.MinDurationMonths}–{range.MaxDurationMonths}).",
            });
        }

        enrolment.ApprovedDurationMonths = months;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}

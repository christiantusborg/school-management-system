namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin-side mirror of the partner subjects+grades endpoint. Returns
/// the subject list for an enrolment's specialization, joined to any
/// <see cref="SubjectGrade"/> rows the partner has already saved.
/// Used by the Admission grade-approval dialog.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/subjects")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsSubjectsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/subjects", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, OdinDbContext db, CancellationToken ct)
    {
        var enrolment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null)
            .Select(e => new { e.SpecializationId, e.Status.Code, ProgrammeCode = e.Specialization.Programmes.Code, SpecName = e.Specialization.Name })
            .FirstOrDefaultAsync(ct);
        if (enrolment is null) return Results.NotFound();

        var existingGrades = await db.Set<SubjectGrade>()
            .Where(g => g.StudentEnrollmentId == enrollmentId)
            .ToDictionaryAsync(g => g.SubjectId, g => g.Score, ct);

        var subjects = await db.Subjects
            .Where(s => s.SpecializationId == enrolment.SpecializationId && s.DeletedAt == null)
            .OrderBy(s => s.Code)
            .Select(s => new
            {
                subjectId = s.SubjectId,
                code = s.Code,
                name = s.Name,
                ects = s.Ects,
            })
            .ToListAsync(ct);

        var items = subjects.Select(s => new
        {
            s.subjectId,
            s.code,
            s.name,
            s.ects,
            score = existingGrades.TryGetValue(s.subjectId, out var sc) ? (int?)sc : null,
        }).ToList();

        return Results.Ok(new
        {
            enrolmentStatus = enrolment.Code,
            programmeCode = enrolment.ProgrammeCode,
            specializationName = enrolment.SpecName,
            items,
        });
    }
}

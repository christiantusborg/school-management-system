namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin students list — drives the Admin Dashboard → Students tab.
/// Returns enrollments grouped by student plus per-status counts the UI shows.
///
/// Each enrolment carries:
///   statusCode — authoritative <see cref="EnrollmentStatus.Code"/> value
///   statusName — human label from <see cref="EnrollmentStatus.Name"/>
/// The frontend filters and styles purely off statusCode; there is no
/// synthetic integer id on the wire.
/// </summary>
[Route("/v1/admin/students")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students", HandleAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        OdinDbContext db,
        CancellationToken cancellationToken,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? programmeId = null,
        [FromQuery] Guid? specializationId = null)
    {
        // Pull every (non-deleted) enrollment + projected status.
        var enrollmentsQuery = db.Enrollments
            .Where(e => e.DeletedAt == null);
        if (partnerId is not null)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.PartnerId == partnerId);
        if (programmeId is not null)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.Specialization.ProgrammeId == programmeId);
        if (specializationId is not null)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.SpecializationId == specializationId);

        var enrollments = await enrollmentsQuery
            .Select(e => new
            {
                e.StudentEnrollmentId,
                e.StudentId,
                e.PartnerId,
                e.SpecializationId,
                e.Specialization.ProgrammeId,
                ProgrammeCode = e.Specialization.Programmes.Code,
                ProgrammeName = e.Specialization.Programmes.Name,
                SpecializationName = e.Specialization.Name,
                e.ModeOfStudy.Name,
                e.CommencementDate,
                StatusCode = e.Status.Code,
                StatusName = e.Status.Name,
                StatusLevel = e.Status.Level,
            })
            .ToListAsync(cancellationToken);

        // Group by student.
        var studentIds = enrollments.Select(e => e.StudentId).Distinct().ToList();
        var students = await db.Students
            .Where(s => studentIds.Contains(s.StudentId) && s.DeletedAt == null)
            .Select(s => new
            {
                s.StudentId,
                s.StudentNumber,
                s.UserId,
                s.PartnerId,
                User = new { s.User.UserName, s.User.Email, s.User.EmailConfirmed },
                Profile = db.UserProfiles.Where(p => p.UserId == s.UserId)
                    .Select(p => new { p.FirstName, p.LastName }).FirstOrDefault(),
                PartnerName = db.Partners.Where(p => p.PartnerId == s.PartnerId).Select(p => p.Name).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var enrollmentsByStudent = enrollments.GroupBy(e => e.StudentId).ToDictionary(g => g.Key, g => g.ToList());

        var items = students.Select(s =>
        {
            var enrs = enrollmentsByStudent.GetValueOrDefault(s.StudentId) ?? new();
            return new
            {
                studentId = s.StudentId,
                studentNumber = s.StudentNumber,
                username = s.User.UserName,
                email = s.User.Email,
                emailVerified = s.User.EmailConfirmed,
                firstName = s.Profile?.FirstName,
                lastName = s.Profile?.LastName,
                partnerId = s.PartnerId,
                partnerName = s.PartnerName,
                enrollments = enrs.Select(e => new
                {
                    studentEnrollmentId = e.StudentEnrollmentId,
                    programmeId = e.ProgrammeId,
                    programmeCode = e.ProgrammeCode,
                    programmeName = e.ProgrammeName,
                    specializationId = e.SpecializationId,
                    specializationName = e.SpecializationName,
                    modeOfStudyName = e.Name,
                    commencementDate = e.CommencementDate,
                    statusCode = e.StatusCode,
                    statusName = e.StatusName,
                }).ToList(),
            };
        }).ToList();

        // Per-statusCode counts span the unfiltered set so chip totals don't
        // collapse when the user clicks a chip. Frontend keys off statusCode.
        var countsByCode = items
            .SelectMany(i => i.enrollments.Select(e => e.statusCode))
            .GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());

        var noEnrolment = items.Count(i => i.enrollments.Count == 0);

        return Results.Ok(new
        {
            items,
            total = items.Count,
            noEnrolment,
            countsByCode,
        });
    }
}

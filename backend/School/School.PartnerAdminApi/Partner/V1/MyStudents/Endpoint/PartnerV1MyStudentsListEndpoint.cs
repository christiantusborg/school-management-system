using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner-scoped students list — drives the "My Students" tab.
///
/// Mirrors <c>AdminV1StudentsListEndpoint</c> but the partner filter is
/// forced to the caller's PartnerId; query-string partnerId is ignored.
///
/// Each enrolment carries <c>statusCode</c> and <c>statusName</c>; the
/// frontend filters and styles purely off statusCode.
/// </summary>
[Route("/v1/partner/my-students")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        OdinDbContext db,
        CancellationToken ct,
        [FromQuery] Guid? programmeId = null,
        [FromQuery] Guid? specializationId = null)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var enrollmentsQuery = db.Enrollments
            .Where(e => e.DeletedAt == null && e.PartnerId == partnerId);
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
                ModeName = e.ModeOfStudy.Name,
                e.CommencementDate,
                StatusCode = e.Status.Code,
                StatusName = e.Status.Name,
            })
            .ToListAsync(ct);

        // Include all of the partner's students even if they have no enrollment yet
        // (frontend "Applying (draft)" chip includes them via includeNoEnrolment).
        var partnerStudents = await db.Students
            .Where(s => s.PartnerId == partnerId && s.DeletedAt == null)
            .Select(s => new
            {
                s.StudentId,
                s.StudentNumber,
                s.UserId,
                s.PartnerId,
                User = new { s.User.UserName, s.User.Email, s.User.EmailConfirmed },
                Profile = db.UserProfiles.Where(p => p.UserId == s.UserId)
                    .Select(p => new { p.FirstName, p.LastName }).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var enrollmentsByStudent = enrollments.GroupBy(e => e.StudentId).ToDictionary(g => g.Key, g => g.ToList());

        var items = partnerStudents.Select(s =>
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
                enrollments = enrs.Select(e => new
                {
                    studentEnrollmentId = e.StudentEnrollmentId,
                    programmeId = e.ProgrammeId,
                    programmeCode = e.ProgrammeCode,
                    programmeName = e.ProgrammeName,
                    specializationId = e.SpecializationId,
                    specializationName = e.SpecializationName,
                    modeOfStudyName = e.ModeName,
                    commencementDate = e.CommencementDate,
                    statusCode = e.StatusCode,
                    statusName = e.StatusName,
                }).ToList(),
            };
        }).ToList();

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

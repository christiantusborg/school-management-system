using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Returns the subject list for an enrolment's specialization, joined to
/// any <see cref="SubjectGrade"/> rows the partner has already saved.
/// Drives the partner's grade-entry form in the Manage modal.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/subjects")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsSubjectsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/subjects", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var enrolment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.PartnerId == partnerId
                && e.DeletedAt == null)
            .Select(e => new { e.SpecializationId, e.Status.Code })
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

        // If admin recently rejected the partner's grade submission, the
        // enrolment is back at AwaitingGradesSubmit and the latest status
        // note carries the rejection reason. Surface it here so the partner
        // sees what to fix in their grade-entry modal.
        //
        // Gate on existing SubjectGrade rows: a real rejection can only
        // occur AFTER the partner first submitted grades (admin reject-grades
        // only fires from AwaitingGradesApproval, which can only be reached
        // by a successful grades submit that creates SubjectGrade rows). A
        // first-time grading session has none, so any status note we'd see
        // there is the admission-approval transition note — not a rejection.
        object? rejection = null;
        if (enrolment.Code == "AwaitingGradesSubmit" && existingGrades.Count > 0)
        {
            var adminRoleId = await db.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync(ct);
            var lastNote = await db.Set<EnrollmentStatusNote>()
                .Where(n => n.EnrollmentId == enrollmentId
                    && n.StatusId == EnrollmentStatusIds.AwaitingGradesSubmit
                    && n.Note != null)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.CreatedAt,
                    n.Note,
                    n.ByUserId,
                })
                .FirstOrDefaultAsync(ct);
            if (lastNote is not null)
            {
                var byUserStr = lastNote.ByUserId.ToString();
                var actorIsAdmin = adminRoleId != null
                    && await db.UserRoles.AnyAsync(ur => ur.UserId == byUserStr && ur.RoleId == adminRoleId, ct);
                string? byName = null;
                if (actorIsAdmin) byName = "Admission Office";
                else
                {
                    var profile = await db.UserProfiles
                        .Where(p => p.UserId == byUserStr)
                        .Select(p => new { p.FirstName, p.LastName }).FirstOrDefaultAsync(ct);
                    if (profile is not null)
                        byName = string.Join(" ", new[] { profile.FirstName, profile.LastName }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));
                }
                rejection = new
                {
                    reason = lastNote.Note,
                    byName = string.IsNullOrWhiteSpace(byName) ? "Admission Office" : byName,
                    atUtc = lastNote.CreatedAt,
                };
            }
        }

        return Results.Ok(new
        {
            enrolmentStatus = enrolment.Code,
            rejection,
            items,
        });
    }
}

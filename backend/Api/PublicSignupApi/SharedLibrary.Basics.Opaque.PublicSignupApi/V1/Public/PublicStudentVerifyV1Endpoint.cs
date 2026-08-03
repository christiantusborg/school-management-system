using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Public;

/// <summary>
/// Public student verification for the embeddable lookup page (iframe on the
/// school website). Exact student-number match only — no browsing, no name
/// search — and only enrolments that are admitted/studying ("Active") or
/// completed ("Graduated") are visible. Internal workflow statuses are never
/// exposed; the completion date shows only once graduated.
/// </summary>
[Route("/v1/public/student-verify")]
[EndpointTag("Public.StudentVerify")]
public sealed class PublicStudentVerifyV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/student-verify", HandleAsync).AllowAnonymous();
        return app;
    }

    // Admitted/studying statuses shown as "Active"; GradesApproved is the
    // pipeline's terminal state and is what the portal calls "Graduated".
    private static readonly Guid[] ActiveStatusIds =
    [
        EnrollmentStatusIds.ApplicationApprovedAdmission,
        EnrollmentStatusIds.AcceptAdmission,
        EnrollmentStatusIds.AwaitingGradesSubmit,
        EnrollmentStatusIds.AwaitingGradesApproval,
    ];

    private static async Task<IResult> HandleAsync(
        OdinDbContext db, CancellationToken ct, [FromQuery] string? studentNumber = null)
    {
        var number = (studentNumber ?? string.Empty).Trim();
        if (number.Length == 0)
            return Results.Ok(new { items = Array.Empty<object>() });

        var normalized = number.ToUpperInvariant();
        var graduatedId = EnrollmentStatusIds.GradesApproved;

        var rows = await db.Enrollments
            .Where(e => e.DeletedAt == null
                && (e.StatusId == graduatedId || ActiveStatusIds.Contains(e.StatusId))
                && db.Students.Any(s => s.StudentId == e.StudentId
                    && s.StudentNumber != null
                    && s.StudentNumber.ToUpper() == normalized))
            .Select(e => new
            {
                e.StatusId,
                e.GraduationDate,
                e.CommencementDate,
                e.ApprovedDurationValue,
                e.ApprovedDurationUnit,
                StudentNumber = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => s.StudentNumber).FirstOrDefault(),
                UserId = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => s.UserId).FirstOrDefault(),
                ProgrammeName = e.Specialization.Programmes.Name,
                MajorName = e.Specialization.Name,
                SpecializationMonths = e.Specialization.DurationOfStudyMonths,
            })
            .ToListAsync(ct);

        var items = new List<object>();
        foreach (var r in rows)
        {
            string name = string.Empty;
            if (r.UserId is { } uid)
            {
                var profile = await db.UserProfiles.Where(p => p.UserId == uid)
                    .Select(p => new { p.FirstName, p.LastName }).FirstOrDefaultAsync(ct);
                name = string.Join(' ', new[] { profile?.FirstName, profile?.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            var graduated = r.StatusId == graduatedId;
            // Actual completion: the Admission-Office graduation date, falling
            // back to the calculated expected completion — same rule as letters.
            DateTime? completion = null;
            if (graduated)
            {
                completion = r.GraduationDate
                    ?? SharedLibrary.Basics.Opaque.Domains.DurationDays.ExpectedCompletion(
                        r.CommencementDate, r.ApprovedDurationValue, r.ApprovedDurationUnit, r.SpecializationMonths);
            }

            items.Add(new
            {
                studentNumber = r.StudentNumber,
                studentName = name,
                status = graduated ? "Graduated" : "Active",
                programme = r.ProgrammeName,
                major = r.MajorName,
                completionDate = completion?.ToString("yyyy-MM-dd"),
            });
        }

        return Results.Ok(new { items });
    }
}

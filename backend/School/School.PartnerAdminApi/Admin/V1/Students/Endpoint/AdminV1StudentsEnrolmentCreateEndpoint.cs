using System.Security.Claims;
namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission Office enrols an EXISTING student into an additional
/// programme/specialization from the drawer's Programs tab. Same
/// availability rule as changing a specialization: the target programme
/// must be core, granted to the student's partner, or owned by it. The new
/// enrolment starts at Awaiting Review by Admission so it enters the normal
/// pipeline (offer letter, acceptance, grading).
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsEnrolmentCreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments", HandleAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/students/enrolment-options", OptionsAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class CreateBody
    {
        public Guid? SpecializationId { get; init; }
        public int ModeOfStudyId { get; init; } = 1;
        /// <summary>Partner (school) the new enrolment belongs to; defaults
        /// to the student's original partner.</summary>
        public Guid? PartnerId { get; init; }
    }

    /// <summary>Programme → specializations a given PARTNER may enrol into:
    /// per-spec granted core specs (not partner-disabled) plus approved specs
    /// of the partner's own custom programmes. Drives the admin drawer's
    /// "+ Add programme" picker after the school is chosen.</summary>
    private static async Task<IResult> OptionsAsync(
        [FromQuery] Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var granted = await db.SpecializationPartners
            .Where(g => g.PartnerId == partnerId && !g.DisabledByPartner
                && g.Specialization.DeletedAt == null
                && g.Specialization.Programmes.DeletedAt == null)
            .Select(g => new
            {
                specializationId = g.SpecializationId,
                specializationName = g.Specialization.Name,
                programmeId = g.Specialization.ProgrammeId,
                programmeName = g.Specialization.Programmes.Name,
                programmeCode = g.Specialization.Programmes.Code,
                schoolName = g.Specialization.Programmes.School != null
                    ? g.Specialization.Programmes.School.Name : null,
            })
            .ToListAsync(ct);
        var ownApproved = await db.PartnerSpecializationStatuses
            .Where(x => x.Status == 2
                && x.Specialization.DeletedAt == null
                && x.Specialization.Programmes.OwnerId == partnerId
                && x.Specialization.Programmes.DeletedAt == null)
            .Select(x => new
            {
                specializationId = x.SpecializationId,
                specializationName = x.Specialization.Name,
                programmeId = x.Specialization.ProgrammeId,
                programmeName = x.Specialization.Programmes.Name,
                programmeCode = x.Specialization.Programmes.Code,
                schoolName = x.Specialization.Programmes.School != null
                    ? x.Specialization.Programmes.School.Name : null,
            })
            .ToListAsync(ct);
        var items = granted.Concat(ownApproved)
            .GroupBy(x => x.programmeId)
            .Select(g => new
            {
                programmeId = g.Key,
                name = g.First().programmeName,
                code = g.First().programmeCode,
                schoolName = g.First().schoolName,
                specializations = g
                    .Select(x => new { x.specializationId, name = x.specializationName })
                    .DistinctBy(x => x.specializationId)
                    .OrderBy(x => x.name)
                    .ToList(),
            })
            .OrderBy(x => x.name)
            .ToList();
        return Results.Ok(new { items });
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, [FromBody] CreateBody body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        if (body.SpecializationId is not { } specId)
            return Results.BadRequest(new { error = "specializationId is required." });

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        var enrolPartnerId = body.PartnerId ?? student.PartnerId;
        if (!await db.Partners.AnyAsync(p => p.PartnerId == enrolPartnerId && p.DeletedAt == null, ct))
            return Results.BadRequest(new { error = "Partner not found." });

        var target = await db.Specializations
            .Where(s => s.SpecializationId == specId && s.DeletedAt == null)
            .Select(s => new { s.SpecializationId, s.ProgrammeId })
            .FirstOrDefaultAsync(ct);
        if (target is null) return Results.BadRequest(new { error = "Specialization not found." });

        // Availability follows the CHOSEN partner's real access: per-spec
        // granted core specs (not disabled) or approved specs of that
        // partner's own custom programmes.
        var available = await db.SpecializationPartners.AnyAsync(g =>
                g.SpecializationId == specId && g.PartnerId == enrolPartnerId && !g.DisabledByPartner, ct)
            || await db.PartnerSpecializationStatuses.AnyAsync(x =>
                x.SpecializationId == specId && x.Status == 2
                && x.Specialization.Programmes.OwnerId == enrolPartnerId, ct);
        if (!available)
            return Results.BadRequest(new { error = "That programme isn't available to the selected partner." });

        var duplicate = await db.Enrollments.AnyAsync(e =>
            e.StudentId == studentId && e.SpecializationId == specId && e.DeletedAt == null, ct);
        if (duplicate)
            return Results.BadRequest(new { error = "The student is already enrolled in this specialization." });

        var enrollmentId = Guid.NewGuid();
        db.Enrollments.Add(new SharedLibrary.Basics.Opaque.Domains.Enrollment
        {
            StudentEnrollmentId = enrollmentId,
            StudentId = studentId,
            PartnerId = enrolPartnerId,
            SpecializationId = specId,
            ModeOfStudyId = body.ModeOfStudyId,
            PathwayId = 0,
            StatusId = SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission,
        });
        db.Set<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote>().Add(
            new SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote
            {
                EnrollmentStatusNoteId = Guid.NewGuid(),
                EnrollmentId = enrollmentId,
                StatusId = SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission,
                Note = "Programme added by Admission Office.",
                ByUserId = Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var g) ? g : Guid.Empty,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { enrollmentId });
    }
}

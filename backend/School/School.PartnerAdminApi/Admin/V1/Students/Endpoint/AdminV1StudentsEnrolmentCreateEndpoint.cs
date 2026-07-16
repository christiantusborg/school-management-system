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
        return app;
    }

    public sealed class CreateBody
    {
        public Guid? SpecializationId { get; init; }
        public int ModeOfStudyId { get; init; } = 1;
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

        var target = await db.Specializations
            .Where(s => s.SpecializationId == specId && s.DeletedAt == null)
            .Select(s => new { s.SpecializationId, s.ProgrammeId })
            .FirstOrDefaultAsync(ct);
        if (target is null) return Results.BadRequest(new { error = "Specialization not found." });

        var owner = await db.Programmes
            .Where(p => p.ProgrammeId == target.ProgrammeId && p.DeletedAt == null)
            .Select(p => new { p.OwnerId })
            .FirstOrDefaultAsync(ct);
        if (owner is null) return Results.BadRequest(new { error = "Programme not found." });

        var available = owner.OwnerId == null
            || owner.OwnerId == student.PartnerId
            || await db.ProgrammePartners.AnyAsync(pp =>
                pp.ProgrammeId == target.ProgrammeId && pp.PartnerId == student.PartnerId && pp.IsActive != null, ct);
        if (!available)
            return Results.BadRequest(new { error = "That programme isn't available to this student's partner." });

        var duplicate = await db.Enrollments.AnyAsync(e =>
            e.StudentId == studentId && e.SpecializationId == specId && e.DeletedAt == null, ct);
        if (duplicate)
            return Results.BadRequest(new { error = "The student is already enrolled in this specialization." });

        var enrollmentId = Guid.NewGuid();
        db.Enrollments.Add(new SharedLibrary.Basics.Opaque.Domains.Enrollment
        {
            StudentEnrollmentId = enrollmentId,
            StudentId = studentId,
            PartnerId = student.PartnerId,
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

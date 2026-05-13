using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Student resubmits an enrolment after addressing rejection feedback.
/// Refuses unless the enrolment is currently in
/// <c>ApplicationRejectedByPartner</c> or <c>ApplicationRejectedByAdmission</c>,
/// and refuses if any required document slot is still unfilled or in a
/// Rejected* state. On success: flips <c>Enrollment.StatusId</c> to
/// <c>ApplicationAwaitingReviewByPartner</c> and writes one
/// <see cref="EnrollmentStatusNote"/>.
/// </summary>
[Route("/v1/student/me/application/{enrollmentId:guid}/resubmit")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeApplicationResubmitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/student/me/application/{enrollmentId:guid}/resubmit", HandleAsync)
            .RequireAuthorization("StudentOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid enrollmentId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.UserId == callerId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        var enrollment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == student.StudentId
                && e.DeletedAt == null)
            .Select(e => new { e.StudentEnrollmentId, e.StatusId, ProgrammeId = e.Specialization.ProgrammeId })
            .FirstOrDefaultAsync(ct);
        if (enrollment is null) return Results.NotFound();

        if (enrollment.StatusId != EnrollmentStatusIds.ApplicationRejectedByPartner
            && enrollment.StatusId != EnrollmentStatusIds.ApplicationRejectedByAdmission)
        {
            return Results.BadRequest(new { error = "This application is not awaiting your changes." });
        }

        // Required slots are sourced from ProgrammeDocumentRequirement for
        // THIS enrolment's programme — no longer hard-coded.
        var requiredTypeIds = await db.ProgrammeDocumentRequirements
            .Where(r => r.ProgrammeId == enrollment.ProgrammeId && r.DeletedAt == null)
            .Select(r => r.DocumentTypeId)
            .ToListAsync(ct);

        // Per-enrolment doc state — never cross-pollinated with sibling apps.
        var docs = await db.StudentDocuments
            .Where(d => d.EnrollmentId == enrollmentId
                && d.DeletedAt == null
                && requiredTypeIds.Contains(d.DocumentTypeId))
            .Select(d => new { d.DocumentTypeId, d.CurrentStatusId })
            .ToListAsync(ct);

        var rejectedStatusIds = new HashSet<Guid> { DocumentStatusIds.RejectedByPartner, DocumentStatusIds.RejectedByEnrolment };
        foreach (var typeId in requiredTypeIds)
        {
            var doc = docs.FirstOrDefault(d => d.DocumentTypeId == typeId);
            if (doc is null)
                return Results.BadRequest(new { error = "Please upload every required document before resubmitting." });
            if (rejectedStatusIds.Contains(doc.CurrentStatusId))
                return Results.BadRequest(new { error = "Please replace every rejected document before resubmitting." });
        }

        // Re-fetch the enrolment for mutation — we only projected above to
        // avoid loading the whole entity tree.
        var enrolmentToMutate = await db.Enrollments
            .FirstAsync(e => e.StudentEnrollmentId == enrollmentId, ct);

        var now = DateTime.UtcNow;
        enrolmentToMutate.StatusId = EnrollmentStatusIds.ApplicationAwaitingReviewByPartner;

        db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
        {
            EnrollmentStatusNoteId = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StatusId = EnrollmentStatusIds.ApplicationAwaitingReviewByPartner,
            Note = "Student resubmitted application after addressing rejection feedback.",
            ByUserId = ParseUserGuid(callerId),
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            enrollmentId,
            statusCode = "ApplicationAwaitingReviewByPartner",
        });
    }

    private static Guid ParseUserGuid(string? userId) =>
        Guid.TryParse(userId, out var g) ? g : Guid.Empty;
}

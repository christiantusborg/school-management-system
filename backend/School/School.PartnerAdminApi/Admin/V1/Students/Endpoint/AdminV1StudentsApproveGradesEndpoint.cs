using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Letters;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin approves the partner's submitted grades. Refuses unless the
/// enrolment is currently in <c>AwaitingGradesApproval</c>. Flips
/// <c>StatusId</c> to <c>GradesApproved</c> (terminal) and writes one
/// <see cref="EnrollmentStatusNote"/>.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/approve-grades")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsApproveGradesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/approve-grades", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        HttpContext httpContext, OdinDbContext db,
        LetterReleaseService letterRelease,
        ILogger<AdminV1StudentsApproveGradesEndpoint> logger,
        CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        if (enrolment.StatusId != EnrollmentStatusIds.AwaitingGradesApproval)
            return Results.BadRequest(new { error = "This enrolment isn't awaiting grade approval." });

        // Reaching this endpoint implies the admin ticked the "tuition fully
        // paid" gate in the UI (the Approve button stays disabled until then).
        // Log it as a separate timeline entry so the activity log shows two
        // distinct steps: payment confirmed → grades approved.
        var actorId = ParseUserGuid(callerId);
        var now = DateTime.UtcNow;

        enrolment.StatusId = EnrollmentStatusIds.GradesApproved;
        db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
        {
            EnrollmentStatusNoteId = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StatusId = EnrollmentStatusIds.GradesApproved,
            Note = "Admission Office confirmed tuition payment.",
            ByUserId = actorId,
            CreatedAt = now,
        });
        db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
        {
            EnrollmentStatusNoteId = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StatusId = EnrollmentStatusIds.GradesApproved,
            Note = "Admission Office approved the submitted grades.",
            ByUserId = actorId,
            // +1ms so chronological sort puts the payment confirm above the
            // grades-approved entry deterministically.
            CreatedAt = now.AddMilliseconds(1),
        });

        await db.SaveChangesAsync(ct);

        // Phase 3: on terminal grades approval, release transcript + certificate.
        // Failures log but don't roll back the approval.
        try
        {
            await letterRelease.ReleaseAsync(enrollmentId, LetterType.Transcript, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Letters] Transcript release failed for enrollment {EnrollmentId}", enrollmentId);
        }
        try
        {
            await letterRelease.ReleaseAsync(enrollmentId, LetterType.Certificate, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Letters] Certificate release failed for enrollment {EnrollmentId}", enrollmentId);
        }
        try
        {
            await letterRelease.ReleaseAsync(enrollmentId, LetterType.ProvisionalCertificate, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Letters] Provisional certificate release failed for enrollment {EnrollmentId}", enrollmentId);
        }

        return Results.Ok(new
        {
            enrollmentId,
            statusCode = "GradesApproved",
        });
    }

    private static Guid ParseUserGuid(string? userId) =>
        Guid.TryParse(userId, out var g) ? g : Guid.Empty;
}

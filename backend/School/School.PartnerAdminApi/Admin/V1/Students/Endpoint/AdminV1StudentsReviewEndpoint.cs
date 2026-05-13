using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Letters;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission Office submits the review for a single enrolment.
///
/// Each approved document moves to <c>VerifiedByEnrolment</c>, each rejected
/// to <c>RejectedByEnrolment</c>; per-doc <see cref="StudentDocumentNote"/>
/// rows record the change. The enrolment's <c>StatusId</c> flips to
/// <c>ApplicationRejectedByAdmission</c> on any reject (sent back to the
/// partner queue) or <c>AcceptOffer</c> when every document is approved
/// (offer presented to the student). One <see cref="EnrollmentStatusNote"/>
/// audit row is appended per transition.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/review")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsReviewEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/review", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class DocumentDecision
    {
        public Guid StudentDocumentId { get; init; }
        public string DocumentLabel { get; init; } = string.Empty;
        public string Decision { get; init; } = "approved";
        public List<string>? Reasons { get; init; }
        public string? FreeTextReason { get; init; }
    }

    public sealed class ReviewRequest
    {
        public List<DocumentDecision>? Documents { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, [FromBody] ReviewRequest body,
        HttpContext httpContext, OdinDbContext db,
        LetterReleaseService letterRelease,
        ILogger<AdminV1StudentsReviewEndpoint> logger,
        CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrollment is null) return Results.NotFound();

        // Status gate: admin can only review applications the partner has
        // already approved (status = ApplicationAwaitingReviewByAdmission).
        // Refuses when the enrolment is awaiting student action (Rejected)
        // or hasn't reached the admission stage yet — same anti-bypass
        // reasoning as the partner endpoint.
        if (enrollment.StatusId != EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission)
        {
            return Results.BadRequest(new
            {
                error = "This application isn't awaiting Admission review. The student must resubmit and the partner must re-approve before it can be reviewed again.",
            });
        }

        var docDecisions = body.Documents ?? new();
        var anyRejected = docDecisions.Any(d => string.Equals(d.Decision, "rejected", StringComparison.OrdinalIgnoreCase));
        var now = DateTime.UtcNow;

        if (docDecisions.Count > 0)
        {
            var docIds = docDecisions.Select(d => d.StudentDocumentId).ToList();
            // Per-application guard: only docs that belong to THIS enrolment
            // are reviewable from THIS wizard. See partner equivalent.
            var docs = await db.StudentDocuments
                .Where(d => d.StudentId == studentId
                    && d.EnrollmentId == enrollmentId
                    && docIds.Contains(d.StudentDocumentId)
                    && d.DeletedAt == null)
                .ToListAsync(ct);

            foreach (var input in docDecisions)
            {
                var doc = docs.FirstOrDefault(x => x.StudentDocumentId == input.StudentDocumentId);
                if (doc is null) continue;
                var isApproved = string.Equals(input.Decision, "approved", StringComparison.OrdinalIgnoreCase);
                var isRejected = string.Equals(input.Decision, "rejected", StringComparison.OrdinalIgnoreCase);
                if (!isApproved && !isRejected) continue;

                var newDocStatusId = isApproved
                    ? DocumentStatusIds.VerifiedByEnrolment
                    : DocumentStatusIds.RejectedByEnrolment;
                doc.CurrentStatusId = newDocStatusId;

                var noteText = isRejected ? BuildDocumentRejectionNote(input) : null;
                db.StudentDocumentNotes.Add(new StudentDocumentNote
                {
                    StudentDocumentNoteId = Guid.NewGuid(),
                    StudentDocumentId = doc.StudentDocumentId,
                    StatusId = newDocStatusId,
                    ByUserId = callerId,
                    Note = noteText,
                    CreatedAt = now,
                });
            }
        }

        // The offer is released at partner-approval; here admission's
        // all-approved transition jumps straight to AwaitingGradesSubmit
        // (skipping the now-retired ApplicationApprovedAdmission and the
        // orphan AcceptAdmission). The Admission Letter PDF still releases
        // here. Partner's next action: submit grades.
        var newEnrollmentStatusId = anyRejected
            ? EnrollmentStatusIds.ApplicationRejectedByAdmission
            : EnrollmentStatusIds.AwaitingGradesSubmit;

        if (enrollment.StatusId != newEnrollmentStatusId)
        {
            enrollment.StatusId = newEnrollmentStatusId;
            db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
            {
                EnrollmentStatusNoteId = Guid.NewGuid(),
                EnrollmentId = enrollmentId,
                StatusId = newEnrollmentStatusId,
                Note = anyRejected
                    ? BuildRejectionNote(docDecisions)
                    : "Admission auto-approved documents and confirmed payment. Admission letter released — awaiting grades from partner.",
                ByUserId = ParseUserGuid(callerId),
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(ct);

        if (!anyRejected)
        {
            try
            {
                await letterRelease.ReleaseAsync(enrollmentId, LetterType.AdmissionLetter, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Letters] Admission letter release failed for enrollment {EnrollmentId}", enrollmentId);
            }
        }

        return Results.Ok(new
        {
            studentId,
            enrollmentId,
            outcome = anyRejected ? "rejected" : "approved",
            statusId = enrollment.StatusId,
        });
    }

    private static string BuildDocumentRejectionNote(DocumentDecision d)
    {
        var reasons = (d.Reasons ?? new()).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        var free = (d.FreeTextReason ?? string.Empty).Trim();
        if (reasons.Count == 0 && string.IsNullOrEmpty(free)) return "(no reason provided)";
        if (reasons.Count > 0 && !string.IsNullOrEmpty(free)) return string.Join(", ", reasons) + " — " + free;
        if (reasons.Count > 0) return string.Join(", ", reasons);
        return free;
    }

    private static string BuildRejectionNote(List<DocumentDecision> decisions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Admission Office rejected the following documents:");
        foreach (var d in decisions.Where(x => string.Equals(x.Decision, "rejected", StringComparison.OrdinalIgnoreCase)))
        {
            var label = string.IsNullOrWhiteSpace(d.DocumentLabel) ? "Document" : d.DocumentLabel.Trim();
            var reasons = (d.Reasons ?? new()).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            var free = (d.FreeTextReason ?? string.Empty).Trim();
            sb.Append("- ").Append(label).Append(": ");
            if (reasons.Count > 0) sb.Append(string.Join(", ", reasons));
            if (!string.IsNullOrEmpty(free))
            {
                if (reasons.Count > 0) sb.Append(" — ");
                sb.Append(free);
            }
            if (reasons.Count == 0 && string.IsNullOrEmpty(free)) sb.Append("(no reason provided)");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private static Guid ParseUserGuid(string? userId) =>
        Guid.TryParse(userId, out var g) ? g : Guid.Empty;
}

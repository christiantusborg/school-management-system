using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Data;
using Odin.Api.Base.Letters;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner submits the review for a single enrolment.
///
/// Each approved document moves to <c>VerifiedByPartner</c> and each
/// rejected document moves to <c>RejectedByPartner</c>; per-doc
/// <see cref="StudentDocumentNote"/> rows record the change. The
/// enrolment's <c>StatusId</c> flips to <c>DocumentRejectedByPartner</c>
/// when any document is rejected, or <c>ReviewedByPartner</c> when every
/// document is approved. Either way one <see cref="EnrollmentStatusNote"/>
/// audit row is appended. Commencement date is saved on the all-approved
/// path.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/review")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsReviewEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/review", HandleAsync)
            .RequireAuthorization("PartnerOnly");
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

    public sealed class EnrolmentBlock
    {
        public DateTime? CommencementDate { get; init; }
        public int? DurationMonths { get; init; }
    }

    public sealed class PaymentBlock
    {
        public string? Type { get; init; }
        public decimal? TuitionFee { get; init; }
        public int? Months { get; init; }
    }

    public sealed class ReviewRequest
    {
        public List<DocumentDecision>? Documents { get; init; }
        public EnrolmentBlock? Enrolment { get; init; }
        public PaymentBlock? Payment { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, [FromBody] ReviewRequest body,
        HttpContext httpContext, OdinDbContext db,
        LetterReleaseService letterRelease,
        ILogger<PartnerV1MyStudentsReviewEndpoint> logger,
        CancellationToken ct)
    {
        var (callerId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.PartnerId == partnerId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.PartnerId == partnerId
                && e.DeletedAt == null, ct);
        if (enrollment is null) return Results.NotFound();

        // Status gate: partner can only review applications currently sitting
        // in their queue. Refuses when the enrolment is awaiting student
        // action (Rejected*) or has moved past the partner stage. Without
        // this gate a partner could approve a previously-rejected doc
        // directly, bypassing the student's replace step entirely.
        if (enrollment.StatusId != EnrollmentStatusIds.ApplicationSubmitted
            && enrollment.StatusId != EnrollmentStatusIds.ApplicationAwaitingReviewByPartner)
        {
            return Results.BadRequest(new
            {
                error = "This application isn't in the partner queue. The student must resubmit before it can be reviewed again.",
            });
        }

        var docDecisions = body.Documents ?? new();
        var anyRejected = docDecisions.Any(d => string.Equals(d.Decision, "rejected", StringComparison.OrdinalIgnoreCase));
        var now = DateTime.UtcNow;

        // Move each affected document to its new status and append an audit-log
        // note. The note carries the actor + (for rejections) the reason text.
        if (docDecisions.Count > 0)
        {
            var docIds = docDecisions.Select(d => d.StudentDocumentId).ToList();
            // Per-application guard: only docs that belong to THIS enrolment
            // are reviewable from THIS wizard. Reviewing BBA must never flip
            // an MBA-scoped row by accident.
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

                var newStatusId = isApproved
                    ? DocumentStatusIds.VerifiedByPartner
                    : DocumentStatusIds.RejectedByPartner;
                doc.CurrentStatusId = newStatusId;

                var noteText = isRejected ? BuildDocumentRejectionNote(input) : null;
                db.StudentDocumentNotes.Add(new StudentDocumentNote
                {
                    StudentDocumentNoteId = Guid.NewGuid(),
                    StudentDocumentId = doc.StudentDocumentId,
                    StatusId = newStatusId,
                    ByUserId = callerId ?? string.Empty,
                    Note = noteText,
                    CreatedAt = now,
                });
            }
        }

        // Single workflow: every approved application waits for the student
        // to accept the offer before admission picks it up. The previous
        // OfferAcceptanceMode branch (AutoAccept skipping AcceptOffer) was
        // removed — the column on Specialization stays for backwards compat
        // but no longer drives the state machine.
        var newEnrollmentStatusId = anyRejected
            ? EnrollmentStatusIds.ApplicationRejectedByPartner
            : EnrollmentStatusIds.AcceptOffer;

        if (enrollment.StatusId != newEnrollmentStatusId)
        {
            enrollment.StatusId = newEnrollmentStatusId;
            var note = anyRejected
                ? BuildRejectionNote(docDecisions)
                : "All documents approved by partner. Offer presented to student.";
            db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
            {
                EnrollmentStatusNoteId = Guid.NewGuid(),
                EnrollmentId = enrollmentId,
                StatusId = newEnrollmentStatusId,
                Note = note,
                ByUserId = ParseUserGuid(callerId),
                // Stagger by a hundred milliseconds so the summary entry
                // sorts strictly after every per-doc verification note in
                // the activity log timeline. (All per-doc rows share `now`.)
                CreatedAt = now.AddMilliseconds(100),
            });
        }

        if (!anyRejected && body.Enrolment is { CommencementDate: { } commencement })
        {
            enrollment.CommencementDate = commencement;
        }

        await db.SaveChangesAsync(ct);

        // Best-effort: release the offer letter PDF after persisting state.
        // Failures here log but never roll back the partner's review.
        if (!anyRejected)
        {
            try
            {
                await letterRelease.ReleaseAsync(enrollmentId, LetterType.OfferLetter, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Letters] Offer letter release failed for enrollment {EnrollmentId}", enrollmentId);
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
        sb.AppendLine("Partner rejected the following documents:");
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

    /// <summary>
    /// EnrollmentStatusNote.ByUserId is Guid but ApplicationUser.Id is string.
    /// Partner-admin user IDs are Identity-issued GUIDs in string form, so this
    /// just parses; falls back to Guid.Empty for non-Guid IDs.
    /// </summary>
    private static Guid ParseUserGuid(string? userId) =>
        Guid.TryParse(userId, out var g) ? g : Guid.Empty;
}

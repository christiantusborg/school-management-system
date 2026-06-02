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

    public sealed class EnrolmentBlock
    {
        public DateTime? CommencementDate { get; init; }
        public int? DurationMonths { get; init; }
    }

    public sealed class ReviewRequest
    {
        public List<DocumentDecision>? Documents { get; init; }
        /// <summary>
        /// Commencement date + duration captured by the wizard. Only honoured
        /// during the partner-stage path (when admin is acting in the partner
        /// queue); the admission-stage path ignores them.
        /// </summary>
        public EnrolmentBlock? Enrolment { get; init; }
    }

    // States the admin can step in for. Includes the partner queue so
    // admin can hand-run the partner stage when the partner is slow.
    private static readonly HashSet<Guid> PartnerStageStatuses = new()
    {
        EnrollmentStatusIds.ApplicationSubmitted,
        EnrollmentStatusIds.ApplicationAwaitingReviewByPartner,
    };

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

        var isPartnerStage = PartnerStageStatuses.Contains(enrollment.StatusId);
        var isAdmissionStage = enrollment.StatusId == EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission;

        if (!isPartnerStage && !isAdmissionStage)
        {
            return Results.BadRequest(new
            {
                error = "This application isn't reviewable in its current status. The student must resubmit before review.",
            });
        }

        var docDecisions = body.Documents ?? new();
        var anyRejected = docDecisions.Any(d => string.Equals(d.Decision, "rejected", StringComparison.OrdinalIgnoreCase));
        var now = DateTime.UtcNow;

        if (docDecisions.Count > 0)
        {
            var docIds = docDecisions.Select(d => d.StudentDocumentId).ToList();
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

                // Partner stage: VerifiedByPartner / RejectedByPartner.
                // Admission stage: VerifiedByEnrolment / RejectedByEnrolment.
                Guid newDocStatusId;
                if (isPartnerStage)
                    newDocStatusId = isApproved ? DocumentStatusIds.VerifiedByPartner : DocumentStatusIds.RejectedByPartner;
                else
                    newDocStatusId = isApproved ? DocumentStatusIds.VerifiedByEnrolment : DocumentStatusIds.RejectedByEnrolment;
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

        // Partner stage routes to AcceptOffer (offer letter) on full approve
        // or ApplicationRejectedByPartner; admission stage routes to
        // AwaitingGradesSubmit (admission letter) on full approve or
        // ApplicationRejectedByAdmission. Both stages keep their existing
        // letter releases.
        Guid newEnrollmentStatusId;
        string approvedNote;
        string rejectedTitle;
        LetterType letterToRelease;
        if (isPartnerStage)
        {
            newEnrollmentStatusId = anyRejected
                ? EnrollmentStatusIds.ApplicationRejectedByPartner
                : EnrollmentStatusIds.AcceptOffer;
            approvedNote = "Admission Office reviewed documents on behalf of the partner. Offer presented to student.";
            rejectedTitle = "Admission Office rejected the following documents (acting on behalf of partner):";
            letterToRelease = LetterType.OfferLetter;
        }
        else
        {
            newEnrollmentStatusId = anyRejected
                ? EnrollmentStatusIds.ApplicationRejectedByAdmission
                : EnrollmentStatusIds.AwaitingGradesSubmit;
            approvedNote = "Admission auto-approved documents and confirmed payment. Admission letter released — awaiting grades from partner.";
            rejectedTitle = "Admission Office rejected the following documents:";
            letterToRelease = LetterType.AdmissionLetter;
        }

        if (enrollment.StatusId != newEnrollmentStatusId)
        {
            enrollment.StatusId = newEnrollmentStatusId;
            db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
            {
                EnrollmentStatusNoteId = Guid.NewGuid(),
                EnrollmentId = enrollmentId,
                StatusId = newEnrollmentStatusId,
                Note = anyRejected
                    ? BuildRejectionNote(docDecisions, rejectedTitle)
                    : approvedNote,
                ByUserId = ParseUserGuid(callerId),
                CreatedAt = now,
            });
        }

        // Partner-stage path also persists commencement + duration the same
        // way the partner review endpoint does.
        if (isPartnerStage && !anyRejected)
        {
            if (body.Enrolment?.CommencementDate is { } commencement)
                enrollment.CommencementDate = commencement;

            if (body.Enrolment?.DurationMonths is int months)
            {
                var range = await db.Specializations
                    .Where(s => s.SpecializationId == enrollment.SpecializationId)
                    .Select(s => new { s.Programmes.MinDurationMonths, s.Programmes.MaxDurationMonths })
                    .FirstOrDefaultAsync(ct);
                if (range is not null
                    && range.MaxDurationMonths > 0
                    && (months < range.MinDurationMonths || months > range.MaxDurationMonths))
                {
                    return Results.BadRequest(new
                    {
                        error = $"Duration {months} months is outside the programme range ({range.MinDurationMonths}–{range.MaxDurationMonths}).",
                    });
                }
                enrollment.ApprovedDurationMonths = months;
            }
        }

        await db.SaveChangesAsync(ct);

        if (!anyRejected)
        {
            try
            {
                await letterRelease.ReleaseAsync(enrollmentId, letterToRelease, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Letters] {LetterType} release failed for enrollment {EnrollmentId}", letterToRelease, enrollmentId);
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

    private static string BuildRejectionNote(List<DocumentDecision> decisions, string title = "Admission Office rejected the following documents:")
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
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

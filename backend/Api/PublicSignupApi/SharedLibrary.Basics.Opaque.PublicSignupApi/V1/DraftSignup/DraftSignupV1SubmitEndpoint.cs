using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Step 6 — final submit. Reads the cached programme selections, materialises
/// `Enrollment` rows with `ApplicationSubmittedAt = DateTime.UtcNow`, advances
/// `Student.WizardStep = 6`, and revokes the wizard token (the application is
/// now sealed; the user can come back via login if they need to amend).
/// </summary>
[Route("/v1/public/draft-signup/submit")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1SubmitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/public/draft-signup/submit", HandleAsync).AllowAnonymous();
        return app;
    }

    public sealed class SubmitRequest
    {
        public bool ConsentProcessing { get; init; }
        public bool ConsentTerms { get; init; }
        public bool ConsentAccuracy { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        HttpContext http,
        [FromBody] SubmitRequest body,
        OdinDbContext db,
        WizardSessionService wizard,
        ITransientStateCache cache,
        CancellationToken ct)
    {
        var session = await WizardTokenAuth.ResolveAsync(http, wizard);
        if (session is null) return WizardTokenAuth.Unauthorised();

        if (!body.ConsentProcessing || !body.ConsentTerms || !body.ConsentAccuracy)
            return Results.BadRequest(new { error = "All three consents are required to submit." });

        var student = await db.Students.FirstOrDefaultAsync(s => s.StudentId == session.StudentId, ct);
        if (student is null) return WizardTokenAuth.Unauthorised();

        var picks = await cache.GetAsync<DraftSignupV1ProgrammesEndpoint.ProgrammeCacheState>(
            DraftSignupV1ProgrammesEndpoint.CacheKey(session.StudentId));
        if (picks is null || picks.Items.Count == 0)
            return Results.BadRequest(new { error = "No programmes selected — go back to step 4." });

        // Attach mode: an existing student getting an ADDITIONAL application.
        // The enrolments belong to the flow's partner (not the student's
        // original partner), the same partner+programme pair is refused, and
        // the partner's earlier documents can carry over verified.
        var wizardTokenHeader = http.Request.Headers[WizardTokenAuth.HeaderName].ToString();
        var attach = await cache.GetAsync<DraftSignupV1AttachEndpoint.AttachState>(
            DraftSignupV1AttachEndpoint.AttachKey(wizardTokenHeader));
        if (attach is not null)
        {
            var pickSpecIds = picks.Items.Select(x => x.SpecializationId).ToList();
            var pickProgrammes = await db.Specializations
                .Where(sp => pickSpecIds.Contains(sp.SpecializationId))
                .Select(sp => new { sp.SpecializationId, sp.ProgrammeId, ProgrammeName = sp.Programmes.Name })
                .ToListAsync(ct);
            foreach (var pp in pickProgrammes)
            {
                var dup = await db.Enrollments.AnyAsync(en => en.StudentId == student.StudentId
                    && en.PartnerId == attach.PartnerId && en.DeletedAt == null
                    && en.Specialization.ProgrammeId == pp.ProgrammeId, ct);
                if (dup)
                    return Results.BadRequest(new { error = $"This student already has an application for {pp.ProgrammeName} with this partner. Pick a different programme." });
            }
        }

        // Digital student card: when opted in and a selected programme issues
        // cards, the card photo upload is mandatory before submitting.
        var pickedProgrammeIds = picks.Items.Select(p => p.ProgrammeId).Distinct().ToList();
        var anyCardProgramme = await db.Programmes
            .AnyAsync(p => pickedProgrammeIds.Contains(p.ProgrammeId) && p.IssueDigitalStudentCard, ct);
        if (anyCardProgramme && student.WantsStudentIdCard)
        {
            var hasCardPhoto = await db.StudentDocuments.AnyAsync(d =>
                d.StudentId == student.StudentId
                && d.DocumentTypeId == SharedLibrary.Basics.Opaque.Domains.SystemDocumentTypeIds.StudentCardPicture
                && d.DeletedAt == null, ct);
            if (!hasCardPhoto)
                return Results.BadRequest(new { error = "Please upload a Student Card Picture (or untick the student card option) — it is required for your digital student ID card." });
        }

        var submittedAt = DateTime.UtcNow;
        var specToEnrolment = new Dictionary<Guid, Guid>();
        foreach (var pick in picks.Items)
        {
            var enrollmentId = Guid.NewGuid();
            specToEnrolment[pick.SpecializationId] = enrollmentId;
            db.Enrollments.Add(new SharedLibrary.Basics.Opaque.Domains.Enrollment
            {
                StudentEnrollmentId = enrollmentId,
                StudentId = student.StudentId,
                PartnerId = attach?.PartnerId ?? student.PartnerId,
                SpecializationId = pick.SpecializationId,
                ModeOfStudyId = pick.ModeOfStudyId,
                // Domain quirk: Enrollment.PathwayId is `int` but Pathway PK
                // is `Guid` — we lose the wizard selection here. Stored as 0.
                PathwayId = 0,
                StatusId = SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.ApplicationSubmitted,
            });
            db.Set<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote>().Add(
                new SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote
                {
                    EnrollmentStatusNoteId = Guid.NewGuid(),
                    EnrollmentId = enrollmentId,
                    StatusId = SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.ApplicationSubmitted,
                    Note = attach is null ? "Submitted by student." : "Additional application submitted (existing student).",
                    ByUserId = Guid.TryParse(student.UserId, out var sg) ? sg : Guid.Empty,
                    CreatedAt = submittedAt,
                });
        }

        // Attribute wizard-uploaded docs to the enrolment that matches their
        // SignupSpecializationId. Any doc whose specialization the student
        // didn't ultimately submit for stays orphaned (EnrollmentId == null,
        // soft-deletable later) — better than guessing.
        var pendingDocs = await db.StudentDocuments
            .Where(d => d.StudentId == student.StudentId
                && d.EnrollmentId == null
                && d.SignupSpecializationId != null
                && d.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var doc in pendingDocs)
        {
            if (doc.SignupSpecializationId is { } specId
                && specToEnrolment.TryGetValue(specId, out var enrId))
            {
                doc.EnrollmentId = enrId;
            }
        }

        student.WizardStep = 6;

        // Attach mode extras: carry the flow partner's earlier documents over
        // to each new enrolment (keeping their verified status) and link a
        // converting CRM lead. Letter-type documents are never carried.
        if (attach is not null)
        {
            if (attach.LinkDocs)
            {
                var letterTypes = new[]
                {
                    SharedLibrary.Basics.Opaque.Domains.SystemDocumentTypeIds.OfferLetter,
                    SharedLibrary.Basics.Opaque.Domains.SystemDocumentTypeIds.AdmissionLetter,
                    SharedLibrary.Basics.Opaque.Domains.SystemDocumentTypeIds.Transcript,
                    SharedLibrary.Basics.Opaque.Domains.SystemDocumentTypeIds.Certificate,
                    SharedLibrary.Basics.Opaque.Domains.SystemDocumentTypeIds.ProvisionalCertificate,
                    SharedLibrary.Basics.Opaque.Domains.SystemDocumentTypeIds.StudentIdCard,
                };
                var sourceDocs = await db.StudentDocuments
                    .Where(d => d.StudentId == student.StudentId && d.DeletedAt == null
                        && d.EnrollmentId != null && !letterTypes.Contains(d.DocumentTypeId)
                        && db.Enrollments.Any(en => en.StudentEnrollmentId == d.EnrollmentId
                            && en.PartnerId == attach.PartnerId && en.DeletedAt == null))
                    .GroupBy(d => d.DocumentTypeId)
                    .Select(g => g.OrderByDescending(d => d.UploadedAt).First())
                    .ToListAsync(ct);
                foreach (var newEnrolmentId in specToEnrolment.Values)
                {
                    foreach (var src in sourceDocs)
                    {
                        db.StudentDocuments.Add(new SharedLibrary.Basics.Opaque.Domains.StudentDocument
                        {
                            StudentId = student.StudentId,
                            DocumentTypeId = src.DocumentTypeId,
                            EnrollmentId = newEnrolmentId,
                            FileName = src.FileName,
                            MimeType = src.MimeType,
                            StoragePath = src.StoragePath,
                            UploadedAt = src.UploadedAt,
                            ExpiryDate = src.ExpiryDate,
                            CurrentStatusId = src.CurrentStatusId,
                            OcrResult = src.OcrResult,
                            AiResult = src.AiResult,
                            AiConfidence = src.AiConfidence,
                            AiFraudRisk = src.AiFraudRisk,
                        });
                    }
                }
            }

            if (Guid.TryParse(attach.CrmLeadId, out var crmLeadId))
            {
                var lead = await db.CrmLeads.FirstOrDefaultAsync(l => l.CrmLeadId == crmLeadId && l.DeletedAt == null, ct);
                if (lead is not null && lead.ConvertedStudentId is null)
                {
                    lead.ConvertedStudentId = student.StudentId;
                    lead.ConvertedAt = DateTime.UtcNow;
                    lead.Status = 1;
                    var wonStage = await db.CrmStages
                        .Where(st => st.CrmPipelineId == lead.CrmPipelineId && st.DeletedAt == null && st.StageType == 1)
                        .OrderBy(st => st.DisplayOrder).FirstOrDefaultAsync(ct);
                    if (wonStage is not null) { lead.CrmStageId = wonStage.CrmStageId; lead.StageEnteredAt = DateTime.UtcNow; }
                    db.CrmActivities.Add(new SharedLibrary.Basics.Opaque.Domains.Crm.CrmActivity
                    {
                        CrmLeadId = lead.CrmLeadId, Kind = 6,
                        Body = $"Converted: additional application for existing student {student.StudentNumber}.",
                        OccurredAt = DateTime.UtcNow,
                        ActorUserId = Guid.TryParse(attach.ActorUserId, out var au) ? au : Guid.Empty,
                        CreatedAt = DateTime.UtcNow,
                    });
                    // The student inherits the lead's full mail history.
                    await Odin.Api.Base.Mail.MailHubService.CopyLeadMailToStudentAsync(db, lead.CrmLeadId, student.StudentId, ct);
                }
            }
            await cache.RemoveAsync(DraftSignupV1AttachEndpoint.AttachKey(wizardTokenHeader));
        }

        await db.SaveChangesAsync(ct);

        // Tear down the transient state.
        await cache.RemoveAsync(DraftSignupV1ProgrammesEndpoint.CacheKey(session.StudentId));
        var token = http.Request.Headers[WizardTokenAuth.HeaderName].ToString();
        await wizard.RevokeAsync(token);

        return Results.Ok(new
        {
            studentId = student.StudentId,
            enrollmentCount = picks.Items.Count,
            applicationSubmittedAt = submittedAt,
        });
    }
}

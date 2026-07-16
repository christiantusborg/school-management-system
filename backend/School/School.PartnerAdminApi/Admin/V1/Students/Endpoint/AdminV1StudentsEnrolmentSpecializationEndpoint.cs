namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission Office changes an enrolment's programme and/or specialization from
/// the student detail (edit) tab, by pointing the enrolment at a different
/// specialization. The target may belong to a different programme, but only one
/// the enrolment's partner actually offers: an IBSS core programme, a programme
/// granted to the partner, or a programme the partner owns. The chosen
/// specialization drives which subjects/grades and letter data apply.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/specialization")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsEnrolmentSpecializationEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/specialization", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class SpecializationBody
    {
        public Guid? SpecializationId { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        [FromBody] SpecializationBody body,
        OdinDbContext db,
        [FromServices] Odin.Api.Base.Letters.LetterReleaseService letterRelease,
        CancellationToken ct)
    {
        if (body.SpecializationId is not { } newSpecId)
            return Results.BadRequest(new { error = "specializationId is required." });

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        var target = await db.Specializations
            .Where(s => s.SpecializationId == newSpecId && s.DeletedAt == null)
            .Select(s => new { s.SpecializationId, s.ProgrammeId, s.Name })
            .FirstOrDefaultAsync(ct);
        if (target is null)
            return Results.BadRequest(new { error = "Specialization not found." });

        // The target programme must be one the enrolment's partner offers:
        // core (no owner), granted via ProgrammePartner, or owned by the partner.
        var programme = await db.Programmes
            .Where(p => p.ProgrammeId == target.ProgrammeId && p.DeletedAt == null)
            .Select(p => new { p.OwnerId })
            .FirstOrDefaultAsync(ct);
        if (programme is null)
            return Results.BadRequest(new { error = "Programme not found." });

        var partnerId = enrolment.PartnerId;
        var available = programme.OwnerId == null
            || programme.OwnerId == partnerId
            || await db.ProgrammePartners.AnyAsync(pp =>
                pp.ProgrammeId == target.ProgrammeId && pp.PartnerId == partnerId && pp.IsActive != null, ct);
        if (!available)
            return Results.BadRequest(new { error = "That programme isn't available to this student's partner." });

        enrolment.SpecializationId = newSpecId;
        await db.SaveChangesAsync(ct);

        // Re-render already-released letters so transcripts/certificates
        // immediately reflect the new programme/specialization (grades of the
        // old specialization are filtered out by the tag resolver). Best
        // effort: a render failure never fails the specialization change.
        var releasedTypes = new (Guid DocTypeId, LetterType Type)[]
        {
            (SystemDocumentTypeIds.OfferLetter,            LetterType.OfferLetter),
            (SystemDocumentTypeIds.AdmissionLetter,        LetterType.AdmissionLetter),
            (SystemDocumentTypeIds.Transcript,             LetterType.Transcript),
            (SystemDocumentTypeIds.PrintableTranscript,    LetterType.PrintableTranscript),
            (SystemDocumentTypeIds.Certificate,            LetterType.Certificate),
            (SystemDocumentTypeIds.ProvisionalCertificate, LetterType.ProvisionalCertificate),
            (SystemDocumentTypeIds.StudentIdCard,          LetterType.StudentIdCard),
        };
        foreach (var (docTypeId, type) in releasedTypes)
        {
            var released = await db.StudentDocuments.AnyAsync(d =>
                d.EnrollmentId == enrollmentId && d.DocumentTypeId == docTypeId && d.DeletedAt == null, ct);
            if (!released) continue;
            try { await letterRelease.ReleaseAsync(enrollmentId, type, ct); }
            catch { /* keep the change even if a re-render fails */ }
        }

        var programmeName = await db.Programmes
            .Where(p => p.ProgrammeId == target.ProgrammeId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct);
        return Results.Ok(new
        {
            programmeId = target.ProgrammeId,
            programmeName,
            specializationId = target.SpecializationId,
            specializationName = target.Name,
        });
    }
}

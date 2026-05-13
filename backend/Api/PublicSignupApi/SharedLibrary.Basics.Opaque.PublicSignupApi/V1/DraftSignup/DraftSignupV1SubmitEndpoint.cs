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
                PartnerId = student.PartnerId,
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
                    Note = "Submitted by student.",
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

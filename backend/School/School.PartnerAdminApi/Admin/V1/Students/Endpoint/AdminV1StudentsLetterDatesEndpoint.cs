using System.Security.Claims;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Letters;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission-Office override for the date printed on the offer / admission
/// letters. Restricted to Administrator and SuperAdministrator (AdminOnly is
/// not enough): backdating a released letter changes an official document. When
/// a date is set and that letter is already released, its PDF is re-rendered so
/// the new date takes effect immediately.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letter-dates")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsLetterDatesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letter-dates", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class LetterDatesBody
    {
        public DateTime? OfferLetterDate { get; init; }
        public DateTime? AdmissionLetterDate { get; init; }
        public DateTime? TranscriptDate { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        [FromBody] LetterDatesBody body,
        HttpContext httpContext,
        [FromServices] UserManager<ApplicationUser> userManager,
        LetterReleaseService letterRelease,
        OdinDbContext db, CancellationToken ct)
    {
        // Administrator+ only: backdating an official letter is sensitive.
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled) return Results.Unauthorized();
        var isAdministrator = await userManager.IsInRoleAsync(caller, AdminLevels.Administrator)
            || await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator);
        if (!isAdministrator)
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        static DateTime? Normalise(DateTime? d) =>
            d is { } v ? DateTime.SpecifyKind(v.Date, DateTimeKind.Unspecified) : null;

        var offerChanged = enrolment.OfferLetterDate != Normalise(body.OfferLetterDate);
        var admissionChanged = enrolment.AdmissionLetterDate != Normalise(body.AdmissionLetterDate);
        var transcriptChanged = enrolment.TranscriptDate != Normalise(body.TranscriptDate);

        enrolment.OfferLetterDate = Normalise(body.OfferLetterDate);
        enrolment.AdmissionLetterDate = Normalise(body.AdmissionLetterDate);
        enrolment.TranscriptDate = Normalise(body.TranscriptDate);
        await db.SaveChangesAsync(ct);

        // Re-render already-released letters so the new date shows immediately.
        // Best-effort: a render failure never fails the date save.
        async Task RegenIfReleased(Guid docTypeId, LetterType type)
        {
            var released = await db.StudentDocuments.AnyAsync(d =>
                d.EnrollmentId == enrollmentId && d.DocumentTypeId == docTypeId && d.DeletedAt == null, ct);
            if (released)
            {
                try { await letterRelease.ReleaseAsync(enrollmentId, type, ct); }
                catch { /* keep the date save even if re-render fails */ }
            }
        }
        if (offerChanged) await RegenIfReleased(SystemDocumentTypeIds.OfferLetter, LetterType.OfferLetter);
        if (admissionChanged) await RegenIfReleased(SystemDocumentTypeIds.AdmissionLetter, LetterType.AdmissionLetter);
        if (transcriptChanged) await RegenIfReleased(SystemDocumentTypeIds.Transcript, LetterType.Transcript);

        return Results.Ok(new
        {
            offerLetterDate = enrolment.OfferLetterDate,
            admissionLetterDate = enrolment.AdmissionLetterDate,
            transcriptDate = enrolment.TranscriptDate,
        });
    }
}

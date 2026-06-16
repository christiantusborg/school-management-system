using System.Security.Claims;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Letters;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Re-releases letters already issued for an enrolment, so the PDFs pick up
/// changed tag data (typically an admin duration override that shifts the
/// completion date). Pass <c>?letterType=OfferLetter</c> to regenerate just
/// one letter (the per-row "Regenerate" button); omit it to regenerate every
/// released type. Only letter types with at least one previously released
/// document are regenerated; nothing is issued for the first time here.
/// Restricted to Administrator and SuperAdministrator, matching the
/// duration-override endpoint that motivates the re-release.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/regenerate")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsRegenerateLettersEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/regenerate", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static readonly (Guid DocumentTypeId, LetterType LetterType)[] LetterDocTypes =
    [
        (SystemDocumentTypeIds.OfferLetter,            LetterType.OfferLetter),
        (SystemDocumentTypeIds.AdmissionLetter,        LetterType.AdmissionLetter),
        (SystemDocumentTypeIds.Transcript,             LetterType.Transcript),
        (SystemDocumentTypeIds.Certificate,            LetterType.Certificate),
        (SystemDocumentTypeIds.ProvisionalCertificate, LetterType.ProvisionalCertificate),
    ];

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId,
        [FromQuery] string? letterType,
        HttpContext httpContext,
        [FromServices] UserManager<ApplicationUser> userManager,
        LetterReleaseService letterRelease,
        OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled)
            return Results.Unauthorized();
        var isAdministrator = await userManager.IsInRoleAsync(caller, AdminLevels.Administrator)
            || await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator);
        if (!isAdministrator)
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        // Optional single-letter filter (the per-row "Regenerate" button).
        LetterType? onlyType = null;
        if (!string.IsNullOrWhiteSpace(letterType))
        {
            if (!Enum.TryParse<LetterType>(letterType, ignoreCase: true, out var parsed))
                return Results.BadRequest(new { error = $"Unknown letter type '{letterType}'." });
            onlyType = parsed;
        }

        var enrolmentExists = await db.Enrollments
            .AnyAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (!enrolmentExists) return Results.NotFound();

        var releasedDocTypeIds = await db.StudentDocuments
            .Where(d => d.EnrollmentId == enrollmentId && d.DeletedAt == null)
            .Select(d => d.DocumentTypeId)
            .Distinct()
            .ToListAsync(ct);

        var regenerated = new List<string>();
        foreach (var (docTypeId, type) in LetterDocTypes)
        {
            if (onlyType is not null && type != onlyType) continue;
            if (!releasedDocTypeIds.Contains(docTypeId)) continue;
            var newDocId = await letterRelease.ReleaseAsync(enrollmentId, type, ct);
            if (newDocId is not null) regenerated.Add(type.ToString());
        }

        return Results.Ok(new { regenerated });
    }
}

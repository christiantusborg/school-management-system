using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Returns everything the wizard saved so far so the page can resume on reload.
/// Pulls live values from the user/profile/student rows plus the cached
/// step-4 programme selections (which are only materialised at /submit).
/// </summary>
[Route("/v1/public/draft-signup/state")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1StateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/draft-signup/state", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext http,
        OdinDbContext db,
        WizardSessionService wizard,
        ITransientStateCache cache,
        CancellationToken ct)
    {
        var session = await WizardTokenAuth.ResolveAsync(http, wizard);
        if (session is null) return WizardTokenAuth.Unauthorised();

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == session.UserId, ct);
        var contactEmail = await db.UserContactEmails
            .Where(e => e.UserId == session.UserId && e.IsPrimary)
            .Select(e => new { e.Email, e.IsVerified })
            .FirstOrDefaultAsync(ct);
        var address = await db.UserAddresses
            .Where(a => a.UserId == session.UserId && a.IsPrimary)
            .FirstOrDefaultAsync(ct);
        var phone = await db.UserPhones
            .Where(p => p.UserId == session.UserId && p.IsPrimary)
            .Select(p => p.Number)
            .FirstOrDefaultAsync(ct);
        var student = await db.Students.FirstOrDefaultAsync(s => s.StudentId == session.StudentId, ct);
        if (student is null) return WizardTokenAuth.Unauthorised();

        // Languages: deferred until UserLanguage.UserId is retyped to string
        // (it's currently `Guid`, which can't FK to ApplicationUser.Id `string`).
        var languages = Array.Empty<object>();

        var documents = await db.StudentDocuments
            .Where(d => d.StudentId == session.StudentId
                && d.EnrollmentId == null
                && d.DeletedAt == null)
            .Select(d => new
            {
                studentDocumentId = d.StudentDocumentId,
                documentTypeId = d.DocumentTypeId,
                specializationId = d.SignupSpecializationId,
                fileName = d.FileName,
                uploadedAt = d.UploadedAt,
            })
            .ToListAsync(ct);

        var programmeSelections = await cache.GetAsync<DraftSignupV1ProgrammesEndpoint.ProgrammeCacheState>(
            $"wizprog:{session.StudentId}");

        return Results.Ok(new
        {
            step = student.WizardStep,
            account = new
            {
                firstName = profile?.FirstName,
                lastName = profile?.LastName,
                email = contactEmail?.Email,
                emailVerified = contactEmail?.IsVerified ?? false,
            },
            personal = new
            {
                dateOfBirth = student.DateOfBirth,
                passportId = student.PassportId,
                nationalityId = student.NationalityId,
                addressLine1 = address?.Street,
                addressLine2 = (string?)null,
                city = address?.City,
                stateRegion = address?.State,
                postalCode = address?.ZipCode,
                countryCode = address?.Country,
                phone,
            },
            background = new
            {
                highestDegree = student.HighestDegree,
                yearsWorkExperience = student.YearsWorkExperience,
                languages,
            },
            programmes = programmeSelections?.Items ?? [],
            documents,
        });
    }
}

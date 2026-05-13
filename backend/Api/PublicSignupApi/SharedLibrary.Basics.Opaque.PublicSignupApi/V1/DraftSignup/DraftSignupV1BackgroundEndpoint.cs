using Odin.Api.Base.Authentication;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Step 3 — background. Persists Student.HighestDegree + YearsWorkExperience.
///
/// The languages array is parsed but NOT persisted yet — `UserLanguage.UserId`
/// is currently `Guid` while `ApplicationUser.Id` is `string`, so EF can't
/// link them. Once you retype `UserLanguage.UserId` to `string` (and rename
/// the PK from `StudentLanguageId` → `UserLanguageId`), the language-replace
/// block below can be uncommented.
/// </summary>
[Route("/v1/public/draft-signup/background")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1BackgroundEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/public/draft-signup/background", HandleAsync).AllowAnonymous();
        return app;
    }

    public sealed class BackgroundRequest
    {
        public string? HighestDegree { get; init; }
        public int? YearsWorkExperience { get; init; }
        public IReadOnlyList<LanguageEntry>? Languages { get; init; }
    }
    public sealed class LanguageEntry
    {
        public int LanguageId { get; init; }
        public int Proficiency { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        HttpContext http,
        [FromBody] BackgroundRequest body,
        OdinDbContext db,
        WizardSessionService wizard,
        CancellationToken ct)
    {
        var session = await WizardTokenAuth.ResolveAsync(http, wizard);
        if (session is null) return WizardTokenAuth.Unauthorised();

        var student = await db.Students.FirstOrDefaultAsync(s => s.StudentId == session.StudentId, ct);
        if (student is null) return WizardTokenAuth.Unauthorised();

        student.HighestDegree = body.HighestDegree;
        student.YearsWorkExperience = body.YearsWorkExperience ?? 0;

        // Languages: deferred — see class summary. To re-enable once
        // UserLanguage is retyped:
        //
        // var existing = await db.UserLanguages
        //     .Where(l => l.UserId == session.UserId)
        //     .ToListAsync(ct);
        // db.UserLanguages.RemoveRange(existing);
        // foreach (var l in body.Languages ?? [])
        //     db.UserLanguages.Add(new UserLanguage
        //     {
        //         UserLanguageId = Guid.NewGuid(),
        //         UserId = session.UserId,
        //         LanguageId = l.LanguageId,
        //         Proficiency = (LanguageProficiency)l.Proficiency,
        //     });

        if (student.WizardStep < 3) student.WizardStep = 3;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { studentId = student.StudentId });
    }
}

using Odin.Api.Base.Letters;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.LetterVerify;

/// <summary>
/// Public letter-authenticity check. Anyone holding a printed IBSS letter can
/// confirm its reference (e.g. <c>IBSS-OL-1A2B3C4D</c>) resolves to a genuine
/// enrolment. The bare code (<c>1A2B3C4D</c>) is also accepted. Returns the
/// same headline facts printed on the letter so the holder can cross-check;
/// an unknown reference returns <c>valid: false</c> rather than 404 so the
/// caller gets a clean answer either way.
/// </summary>
[Route("/v1/public/letters/verify/{reference}")]
[EndpointTag("Public.Letters")]
public sealed class LetterVerifyV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/letters/verify/{reference}", HandleAsync).AllowAnonymous();
        return app;
    }

    private static readonly IReadOnlyDictionary<string, string> TypeLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["OL"]    = "Offer Letter",
            ["AL"]    = "Admission Letter",
            ["TR"]    = "Transcript",
            ["CERT"]  = "Certificate",
            ["PCERT"] = "Provisional Certificate",
        };

    private static async Task<IResult> HandleAsync(
        string reference,
        OdinDbContext db,
        LetterTagResolver tagResolver,
        CancellationToken ct)
    {
        // Accept "IBSS-OL-1A2B3C4D", "OL-1A2B3C4D", or the bare "1A2B3C4D".
        var parts = reference.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return Results.Ok(new { valid = false });
        var code = parts[^1].ToUpperInvariant();
        var typeCode = parts.Length >= 2 ? parts[^2] : null;
        var letterType = typeCode is not null && TypeLabels.TryGetValue(typeCode, out var label)
            ? label
            : null;

        var enrollmentId = await db.Enrollments
            .Where(e => e.LetterReferenceCode == code && e.DeletedAt == null)
            .Select(e => e.StudentEnrollmentId)
            .FirstOrDefaultAsync(ct);
        if (enrollmentId == Guid.Empty) return Results.Ok(new { valid = false });

        var tags = await tagResolver.ResolveAsync(enrollmentId, ct);
        return Results.Ok(new
        {
            valid = true,
            reference = reference.ToUpperInvariant(),
            letterType,
            studentName     = tags["[student full name]"],
            programme       = tags["[program name]"],
            specialization  = tags["[specialization name]"],
            commencementDate = tags["[commencement date]"],
            completionDate  = tags["[completion date]"],
            durationOfStudy = tags["[duration of study]"],
        });
    }
}

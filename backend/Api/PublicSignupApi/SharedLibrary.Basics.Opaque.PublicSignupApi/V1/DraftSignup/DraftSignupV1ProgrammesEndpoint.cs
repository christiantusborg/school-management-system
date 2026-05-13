using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Step 4 — programmes. Programme picks live in the wizard cache (NOT in
/// `Enrollment` rows yet), keyed by StudentId. /submit reads them back and
/// materialises real Enrollment rows with `ApplicationSubmittedAt = UtcNow`.
/// </summary>
[Route("/v1/public/draft-signup/programmes")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1ProgrammesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/v1/public/draft-signup/programmes", HandleAsync).AllowAnonymous();
        return app;
    }

    public sealed class ProgrammesRequest
    {
        public IReadOnlyList<ProgrammeItem>? Items { get; init; }
    }
    public sealed class ProgrammeItem
    {
        public Guid ProgrammeId { get; init; }
        public Guid SpecializationId { get; init; }
        public int ModeOfStudyId { get; init; }
        // PathwayId is `Guid?` to match the Pathway entity / catalogue. The
        // domain quirk (Enrollment.PathwayId is `int`) means /submit drops
        // this selection when materialising the Enrollment row — Enrollment
        // gets PathwayId = 0 there. When you retype Enrollment.PathwayId to
        // `Guid`, /submit can stop dropping it.
        public Guid? PathwayId { get; init; }
    }
    public sealed record ProgrammeCacheState(IReadOnlyList<ProgrammeItem> Items);

    public static string CacheKey(Guid studentId) => $"wizprog:{studentId}";

    private static async Task<IResult> HandleAsync(
        HttpContext http,
        [FromBody] ProgrammesRequest body,
        OdinDbContext db,
        WizardSessionService wizard,
        ITransientStateCache cache,
        CancellationToken ct)
    {
        var session = await WizardTokenAuth.ResolveAsync(http, wizard);
        if (session is null) return WizardTokenAuth.Unauthorised();

        var items = (body.Items ?? Array.Empty<ProgrammeItem>())
            .Where(i => i.ProgrammeId != Guid.Empty && i.SpecializationId != Guid.Empty)
            .ToList();

        await cache.SetAsync(CacheKey(session.StudentId),
            new ProgrammeCacheState(items),
            WizardSessionService.Ttl);

        var student = await db.Students.FirstOrDefaultAsync(s => s.StudentId == session.StudentId, ct);
        if (student is not null && student.WizardStep < 4)
        {
            student.WizardStep = 4;
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(new { count = items.Count });
    }
}

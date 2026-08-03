using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.ProgrammeAccess.Endpoint;

/// <summary>
/// Drives the partner portal "My Core Programmes" tab.
///
/// GET lists the specializations IBSS admin has granted to the calling
/// partner (per-spec `SpecializationPartner` rows), including the ones the
/// partner has switched off (`disabledByPartner: true`, rendered dimmed).
///
/// PATCH toggles `DisabledByPartner` on the single spec grant — siblings
/// are untouched and the admin grant itself is never removed. Disabled
/// specs are hidden from the public signup catalogue.
/// </summary>
[Route("/v1/partner/programme-access")]
[EndpointTag("Partner.ProgrammeAccess")]
public sealed class PartnerV1ProgrammeAccessEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/programme-access", ListAsync).RequireAuthorization("PartnerOnly");
        app.MapPatch("/v1/partner/programme-access/{specializationId:guid}", ToggleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class ProgrammeAccessToggleRequest
    {
        public bool Disabled { get; init; }
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var items = await db.SpecializationPartners
            .Where(g => g.PartnerId == partnerId && g.Specialization.DeletedAt == null)
            .OrderBy(g => g.Specialization.Programmes.Name)
            .ThenBy(g => g.Specialization.Name)
            .Select(g => new
            {
                specializationId = g.SpecializationId,
                programmeId = g.Specialization.ProgrammeId,
                programmeName = g.Specialization.Programmes.Name,
                schoolName = g.Specialization.Programmes.School != null ? g.Specialization.Programmes.School.Name : null,
                specializationName = g.Specialization.Name,
                disabledByPartner = g.DisabledByPartner,
            })
            .ToListAsync(ct);

        return Results.Ok(new { items, total = items.Count });
    }

    private static async Task<IResult> ToggleAsync(
        Guid specializationId, [FromBody] ProgrammeAccessToggleRequest body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var programmeId = await db.Specializations
            .Where(s => s.SpecializationId == specializationId)
            .Select(s => (Guid?)s.ProgrammeId)
            .FirstOrDefaultAsync(ct);
        if (programmeId is null) return Results.NotFound();

        var grant = await db.SpecializationPartners
            .FirstOrDefaultAsync(g => g.PartnerId == partnerId && g.SpecializationId == specializationId, ct);
        if (grant is null)
            return Results.NotFound(new { error = "This specialization is not granted to your partner." });

        grant.DisabledByPartner = body.Disabled;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { disabled = grant.DisabledByPartner });
    }
}

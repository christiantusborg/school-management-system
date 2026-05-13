using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.ProgrammeAccess.Endpoint;

/// <summary>
/// Drives the partner portal "My Core Programmes" tab.
///
/// GET lists every specialization under each programme that IBSS admin has
/// granted to the calling partner.
///
/// PATCH toggles `disabled`. The toggle is **lossy at the programme level**
/// because the domain stores access via `ProgrammePartner` (programme-scope),
/// not per-specialization. Setting `disabled=true` on any spec revokes the
/// whole programme grant — so every sibling specialization disappears from
/// subsequent list responses. Setting `disabled=false` re-grants the
/// programme.
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

        var grantedProgrammeIds = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partnerId && pp.IsActive != null)
            .Select(pp => pp.ProgrammeId)
            .ToListAsync(ct);

        var items = await db.Specializations
            .Where(s => s.DeletedAt == null && grantedProgrammeIds.Contains(s.ProgrammeId))
            .OrderBy(s => s.Programmes.Name)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                specializationId = s.SpecializationId,
                programmeId = s.ProgrammeId,
                programmeName = s.Programmes.Name,
                specializationName = s.Name,
                disabledByPartner = false,
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

        var existing = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partnerId && pp.ProgrammeId == programmeId.Value)
            .ToListAsync(ct);

        if (body.Disabled)
        {
            if (existing.Count == 0) return Results.Ok(new { disabled = true, removed = 0 });
            db.ProgrammePartners.RemoveRange(existing);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { disabled = true, removed = existing.Count });
        }

        if (existing.Count > 0)
        {
            foreach (var pp in existing.Where(e => e.IsActive == null))
                pp.IsActive = DateTime.UtcNow;
        }
        else
        {
            db.ProgrammePartners.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.ProgrammePartner
            {
                ProgrammePartnerId = Guid.NewGuid(),
                PartnerId = partnerId!.Value,
                ProgrammeId = programmeId.Value,
                IsActive = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { disabled = false });
    }
}

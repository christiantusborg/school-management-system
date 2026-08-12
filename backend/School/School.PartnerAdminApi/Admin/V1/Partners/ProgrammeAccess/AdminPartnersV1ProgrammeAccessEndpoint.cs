using Odin.Api.Base.Authorization;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Partners.ProgrammeAccess;

/// <summary>
/// Drives the Admin → Partners → Manage → "Core Programmes" tab.
///
/// Grants are stored per SPECIALIZATION (`SpecializationPartner`); ticking
/// or unticking one spec affects only that spec. The programme-level
/// `ProgrammePartner` row is maintained as a derived umbrella (present
/// while the partner holds at least one spec grant) because enrolment,
/// import and cohort validation check the programme grant.
/// </summary>
[Route("/v1/admin/school/partners/{partnerId:guid}/programme-access")]
[EndpointTag("Admin.Partners.ProgrammeAccess")]
public sealed class AdminPartnersV1ProgrammeAccessEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/school/partners/{partnerId:guid}/programme-access", ListAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/school/partners/{partnerId:guid}/programme-access", GrantAsync)
            .RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/school/partners/{partnerId:guid}/programme-access/{specializationId:guid}", RevokeAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class GrantRequest
    {
        public IReadOnlyList<Guid>? SpecializationIds { get; init; }
    }

    private static async Task<IResult> ListAsync(
        Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();

        var items = await db.SpecializationPartners
            .Where(g => g.PartnerId == partnerId && g.Specialization.DeletedAt == null)
            .Select(g => new
            {
                specializationId = g.SpecializationId,
                programmeId = g.Specialization.ProgrammeId,
                code = g.Specialization.Code,
                name = g.Specialization.Name,
                disabledByPartner = g.DisabledByPartner,
            })
            .ToListAsync(ct);

        return Results.Ok(new { items, total = items.Count });
    }

    private static async Task<IResult> GrantAsync(
        Guid partnerId, [FromBody] GrantRequest body, OdinDbContext db,
        HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.programmes.core_grant", ct) != AccessLevel.Edit) return Results.Forbid();

        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();
        var ids = body.SpecializationIds ?? Array.Empty<Guid>();
        if (ids.Count == 0) return Results.Ok(new { granted = 0 });

        var specs = await db.Specializations
            .Where(s => ids.Contains(s.SpecializationId) && s.DeletedAt == null)
            .Select(s => new { s.SpecializationId, s.ProgrammeId })
            .ToListAsync(ct);

        var existingSpecGrants = await db.SpecializationPartners
            .Where(g => g.PartnerId == partnerId && ids.Contains(g.SpecializationId))
            .ToListAsync(ct);
        var existingSpecIds = existingSpecGrants.Select(g => g.SpecializationId).ToHashSet();
        foreach (var g in existingSpecGrants.Where(g => g.DisabledByPartner))
            g.DisabledByPartner = false; // admin re-grant overrides a partner opt-out

        foreach (var spec in specs.Where(x => !existingSpecIds.Contains(x.SpecializationId)))
        {
            db.SpecializationPartners.Add(new SpecializationPartner
            {
                SpecializationId = spec.SpecializationId,
                PartnerId = partnerId,
                DisabledByPartner = false,
                GrantedAt = DateTime.UtcNow,
            });
        }

        // Maintain the programme-level umbrella row.
        var programmeIds = specs.Select(x => x.ProgrammeId).Distinct().ToList();
        var existing = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partnerId && programmeIds.Contains(pp.ProgrammeId))
            .ToListAsync(ct);
        var existingProgrammeIds = existing.Select(e => e.ProgrammeId).ToHashSet();
        foreach (var pp in existing.Where(e => e.IsActive == null))
            pp.IsActive = DateTime.UtcNow;
        foreach (var newProgrammeId in programmeIds.Where(p => !existingProgrammeIds.Contains(p)))
        {
            db.ProgrammePartners.Add(new ProgrammePartner
            {
                ProgrammePartnerId = Guid.NewGuid(),
                PartnerId = partnerId,
                ProgrammeId = newProgrammeId,
                IsActive = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { granted = specs.Count });
    }

    private static async Task<IResult> RevokeAsync(
        Guid partnerId, Guid specializationId, OdinDbContext db,
        HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.programmes.core_grant", ct) != AccessLevel.Edit) return Results.Forbid();

        var programmeId = await db.Specializations
            .Where(s => s.SpecializationId == specializationId)
            .Select(s => (Guid?)s.ProgrammeId)
            .FirstOrDefaultAsync(ct);
        if (programmeId is null) return Results.NotFound();

        var rows = await db.SpecializationPartners
            .Where(g => g.PartnerId == partnerId && g.SpecializationId == specializationId)
            .ToListAsync(ct);
        db.SpecializationPartners.RemoveRange(rows);

        // Drop the programme umbrella when this was the last spec grant.
        var siblingsLeft = await db.SpecializationPartners
            .AnyAsync(g => g.PartnerId == partnerId
                && g.SpecializationId != specializationId
                && g.Specialization.ProgrammeId == programmeId.Value, ct);
        if (!siblingsLeft)
        {
            var umbrella = await db.ProgrammePartners
                .Where(pp => pp.PartnerId == partnerId && pp.ProgrammeId == programmeId.Value)
                .ToListAsync(ct);
            db.ProgrammePartners.RemoveRange(umbrella);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { revoked = rows.Count });
    }
}

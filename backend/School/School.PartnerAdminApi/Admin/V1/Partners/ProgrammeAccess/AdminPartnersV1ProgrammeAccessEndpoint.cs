namespace School.PartnerAdminApi.Admin.V1.Partners.ProgrammeAccess;

/// <summary>
/// Drives the Admin → Partners → Manage → "Core Programmes" tab.
///
/// Frontend toggles access at the SPECIALIZATION level. The domain stores
/// access at the PROGRAMME level via `ProgrammePartner`. We bridge: granting
/// a specialization grants the parent programme to the partner; the GET
/// returns every specialization under each granted programme.
///
/// Lossy on partial revoke: removing one specialization removes the whole
/// programme grant (no per-spec storage exists). The frontend currently only
/// removes one at a time, so this is acceptable until you ask for finer
/// granularity.
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

        var grantedProgrammeIds = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partnerId && pp.IsActive != null)
            .Select(pp => pp.ProgrammeId)
            .ToListAsync(ct);

        var items = await db.Specializations
            .Where(s => s.DeletedAt == null && grantedProgrammeIds.Contains(s.ProgrammeId))
            .Select(s => new
            {
                specializationId = s.SpecializationId,
                programmeId = s.ProgrammeId,
                code = s.Code,
                name = s.Name,
            })
            .ToListAsync(ct);

        return Results.Ok(new { items, total = items.Count });
    }

    private static async Task<IResult> GrantAsync(
        Guid partnerId, [FromBody] GrantRequest body, OdinDbContext db, CancellationToken ct)
    {
        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();
        var ids = body.SpecializationIds ?? Array.Empty<Guid>();
        if (ids.Count == 0) return Results.Ok(new { granted = 0 });

        var programmeIds = await db.Specializations
            .Where(s => ids.Contains(s.SpecializationId) && s.DeletedAt == null)
            .Select(s => s.ProgrammeId)
            .Distinct()
            .ToListAsync(ct);

        var existing = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partnerId && programmeIds.Contains(pp.ProgrammeId))
            .ToListAsync(ct);
        var existingProgrammeIds = existing.Select(e => e.ProgrammeId).ToHashSet();
        foreach (var pp in existing.Where(e => e.IsActive == null))
            pp.IsActive = DateTime.UtcNow;

        foreach (var newProgrammeId in programmeIds.Where(p => !existingProgrammeIds.Contains(p)))
        {
            db.ProgrammePartners.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.ProgrammePartner
            {
                ProgrammePartnerId = Guid.NewGuid(),
                PartnerId = partnerId,
                ProgrammeId = newProgrammeId,
                IsActive = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { granted = programmeIds.Count });
    }

    private static async Task<IResult> RevokeAsync(
        Guid partnerId, Guid specializationId, OdinDbContext db, CancellationToken ct)
    {
        var programmeId = await db.Specializations
            .Where(s => s.SpecializationId == specializationId)
            .Select(s => (Guid?)s.ProgrammeId)
            .FirstOrDefaultAsync(ct);
        if (programmeId is null) return Results.NotFound();

        var rows = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partnerId && pp.ProgrammeId == programmeId.Value)
            .ToListAsync(ct);
        if (rows.Count == 0) return Results.Ok(new { revoked = 0 });

        db.ProgrammePartners.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { revoked = rows.Count });
    }
}

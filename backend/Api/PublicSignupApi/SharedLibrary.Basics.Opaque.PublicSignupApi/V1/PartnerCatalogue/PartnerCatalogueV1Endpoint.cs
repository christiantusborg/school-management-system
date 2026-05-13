using SharedLibrary.Basics.Opaque.Domains.Partners;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.PartnerCatalogue;

/// <summary>
/// Public catalogue served by the signup wizard. Returns the partner identity
/// plus the programmes the partner accepts applications for (IBSS core ∪
/// programmes wired via ProgrammePartner) along with — per programme — the
/// pathways that lead into it (with min-years and accepted prior degrees and
/// required documents) and the modes-of-study the wizard surfaces.
///
/// The wizard reads `catalogue.programmes[].pathways[]` directly when
/// computing the years-of-experience cap, the per-pathway document set and
/// the degree-gating filter — so each pathway entry must carry the full
/// per-pathway metadata, not just an ID.
/// </summary>
[Route("/v1/public/partner/{slug}/catalogue")]
[EndpointTag("Public.PartnerCatalogue")]
public sealed class PartnerCatalogueV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/partner/{slug}/catalogue", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(OdinDbContext db, string slug, CancellationToken ct)
    {
        var partner = await db.Partners
            .Where(p => p.Slug == slug && p.DeletedAt == null)
            .Select(p => new { p.PartnerId, p.Name, p.Slug })
            .FirstOrDefaultAsync(ct);
        if (partner is null) return Results.NotFound(new { error = "Partner not found." });

        // Core programmes (no owner) ∪ programmes explicitly granted to this partner.
        var grantedProgrammeIds = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partner.PartnerId && pp.IsActive != null)
            .Select(pp => pp.ProgrammeId)
            .ToListAsync(ct);

        var programmes = await db.Programmes
            .Where(p => p.DeletedAt == null
                        && (p.OwnerId == null || grantedProgrammeIds.Contains(p.ProgrammeId)))
            .OrderBy(p => p.Code)
            .Select(p => new
            {
                p.ProgrammeId,
                p.Code,
                p.Name,
                p.Description,
                p.AwardEducationLevelId,
                AwardRank = p.AwardEducationLevel != null ? (int?)p.AwardEducationLevel.Rank : null,
            })
            .ToListAsync(ct);

        var programmeIds = programmes.Select(p => p.ProgrammeId).ToList();

        var specs = await db.Specializations
            .Where(s => s.DeletedAt == null && programmeIds.Contains(s.ProgrammeId))
            .OrderBy(s => s.Code)
            .Select(s => new
            {
                s.SpecializationId,
                s.ProgrammeId,
                s.Code,
                s.Name,
                s.Description,
                s.DurationOfStudyMonths,
            })
            .ToListAsync(ct);
        var specsByProgramme = specs.GroupBy(s => s.ProgrammeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Pathways linked to programmes via ProgrammePathway.
        var programmePathwayLinks = await db.ProgrammePathways
            .Where(pp => pp.DeletedAt == null && programmeIds.Contains(pp.ProgrammeId))
            .Select(pp => new { pp.ProgrammeId, pp.PathwayId })
            .ToListAsync(ct);
        var pathwayIdsByProgramme = programmePathwayLinks
            .GroupBy(x => x.ProgrammeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PathwayId).ToList());

        var allLinkedPathwayIds = programmePathwayLinks.Select(x => x.PathwayId).Distinct().ToList();
        var pathways = await db.Pathways
            .Where(p => allLinkedPathwayIds.Contains(p.PathwayId) && p.DeletedAt == null)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.PathwayId,
                p.Name,
                p.Description,
                p.MinimumYearsWorkExperience,
            })
            .ToListAsync(ct);
        var pathwayById = pathways.ToDictionary(p => p.PathwayId);

        var acceptedLevels = await db.PathwayAcceptedEducationLevels
            .Where(a => allLinkedPathwayIds.Contains(a.PathwayId))
            .Select(a => new { a.PathwayId, a.EducationLevelId })
            .ToListAsync(ct);
        var levelsByPathway = acceptedLevels
            .GroupBy(x => x.PathwayId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.EducationLevelId).ToList());

        var docRequirements = await db.PathwayDocumentRequirements
            .Where(d => d.DeletedAt == null && allLinkedPathwayIds.Contains(d.PathwayId))
            .Select(d => new { d.PathwayId, d.DocumentTypeId })
            .ToListAsync(ct);
        var docsByPathway = docRequirements
            .GroupBy(x => x.PathwayId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DocumentTypeId).ToList());

        var modesOfStudy = await db.ModesOfStudy
            .Where(m => m.DeletedAt == null)
            .OrderBy(m => m.Name)
            .Select(m => new { m.ModeOfStudyId, m.Name })
            .ToListAsync(ct);

        var programmeItems = programmes.Select(p =>
        {
            var pids = pathwayIdsByProgramme.TryGetValue(p.ProgrammeId, out var v) ? v : new List<Guid>();
            var nestedPathways = pids.Where(pathwayById.ContainsKey).Select(pid =>
            {
                var pw = pathwayById[pid];
                return new
                {
                    pathwayId = pw.PathwayId,
                    name = pw.Name,
                    description = pw.Description,
                    minimumYearsWorkExperience = pw.MinimumYearsWorkExperience,
                    acceptedEducationLevelIds = levelsByPathway.TryGetValue(pid, out var lvls) ? lvls : new(),
                    requiredDocumentTypeIds = docsByPathway.TryGetValue(pid, out var dts) ? dts : new(),
                };
            }).ToList();

            return new
            {
                programmeId = p.ProgrammeId,
                code = p.Code,
                name = p.Name,
                description = p.Description,
                awardEducationLevelId = p.AwardEducationLevelId,
                awardRank = p.AwardRank,
                specializations = specsByProgramme.TryGetValue(p.ProgrammeId, out var sps)
                    ? sps.Select(s => new
                      {
                          specializationId = s.SpecializationId,
                          code = s.Code,
                          name = s.Name,
                          description = s.Description,
                          durationOfStudyMonths = s.DurationOfStudyMonths,
                      }).ToList()
                    : [],
                pathways = nestedPathways,
                // Programme-wide required docs (union across its pathways) — used
                // by the wizard when no pathway is chosen.
                requiredDocumentTypeIds = nestedPathways
                    .SelectMany(pw => pw.requiredDocumentTypeIds)
                    .Distinct().ToList(),
            };
        }).ToList();

        return Results.Ok(new
        {
            partner = new { partnerId = partner.PartnerId, name = partner.Name, slug = partner.Slug },
            programmes = programmeItems,
            modesOfStudy = modesOfStudy.Select(m => new { modeOfStudyId = m.ModeOfStudyId, name = m.Name }),
        });
    }
}

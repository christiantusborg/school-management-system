namespace School.ProgrammeApi;

/// <summary>
/// Admin Academic catalogue: list programmes filtered by ownership / soft-delete.
/// `ownership=core` → IBSS core programmes (OwnerId is null).
/// `ownership=partner` → partner-owned programmes (OwnerId is set).
/// Default: all (no filter).
/// `deleted=true` → only soft-deleted rows.
///
/// Each item includes `pathwayIds` so the Academic admin UI can render the
/// pathway-toggle checkboxes without N+1 fetches.
/// </summary>
[Route("/v1/school/programmes")]
[EndpointTag("School.Programmes")]
public sealed class SchoolProgrammesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/school/programmes", HandleAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        OdinDbContext db,
        CancellationToken cancellationToken,
        [FromQuery] string? ownership = null,
        [FromQuery] bool deleted = false)
    {
        var q = db.Programmes.AsQueryable();
        q = deleted
            ? q.Where(p => p.DeletedAt != null)
            : q.Where(p => p.DeletedAt == null);

        if (string.Equals(ownership, "core", StringComparison.OrdinalIgnoreCase))
            q = q.Where(p => p.OwnerId == null);
        else if (string.Equals(ownership, "partner", StringComparison.OrdinalIgnoreCase))
            q = q.Where(p => p.OwnerId != null);

        var rows = await q
            .OrderBy(p => p.Code)
            .Select(p => new
            {
                p.ProgrammeId,
                p.Code,
                p.Name,
                p.Description,
                p.OwnerId,
                p.AwardEducationLevelId,
                p.DeletedAt,
                // Partner-programme status (null for core programmes — they
                // never have a PartnerProgrammeStatus row). The admin's
                // partner-manage drawer needs this so its Custom Programmes
                // tab can render status pills + actions for the partner's
                // pending/approved/rejected programmes.
                Status = db.PartnerProgrammeStatuses
                    .Where(s => s.ProgrammeId == p.ProgrammeId)
                    .Select(s => new { s.Status, s.IsActive, s.IsDisabledByAdmin, s.RejectionReason })
                    .FirstOrDefault(),
                HasEnrolments = db.Enrollments
                    .Any(e => e.DeletedAt == null && e.Specialization.ProgrammeId == p.ProgrammeId),
            })
            .ToListAsync(cancellationToken);

        var programmeIds = rows.Select(r => r.ProgrammeId).ToList();
        var pathwaysByProgramme = await db.ProgrammePathways
            .Where(pp => pp.DeletedAt == null && programmeIds.Contains(pp.ProgrammeId))
            .Select(pp => new { pp.ProgrammeId, pp.PathwayId })
            .ToListAsync(cancellationToken);
        var pathwayMap = pathwaysByProgramme
            .GroupBy(x => x.ProgrammeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PathwayId).ToList());

        var items = rows.Select(r => new
        {
            programmeId = r.ProgrammeId,
            code = r.Code,
            name = r.Name,
            description = r.Description,
            ownerId = r.OwnerId,
            awardEducationLevelId = r.AwardEducationLevelId,
            deletedAt = r.DeletedAt,
            pathwayIds = pathwayMap.TryGetValue(r.ProgrammeId, out var ids) ? ids : new List<Guid>(),
            status = r.Status is null ? "Draft" : (r.Status.Status switch
            {
                0 => "Draft",
                1 => "Pending",
                2 => "Approved",
                3 => "Rejected",
                _ => "Draft",
            }),
            isActive = r.Status?.IsActive ?? false,
            isDisabledByAdmin = r.Status?.IsDisabledByAdmin ?? false,
            rejectionReason = r.Status?.RejectionReason,
            hasEnrolments = r.HasEnrolments,
        }).ToList();

        return Results.Ok(new { items, total = items.Count });
    }
}

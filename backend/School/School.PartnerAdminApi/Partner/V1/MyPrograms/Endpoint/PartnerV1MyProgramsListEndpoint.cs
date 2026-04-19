namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, EndpointHandlerAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        [FromServices] OdinDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var (_, partnerIdOrNull) = await MyProgramsHelpers.ResolvePartnerAsync(httpContext, db, ct);
        if (partnerIdOrNull is null) return Results.Forbid();
        var partnerId = partnerIdOrNull.Value;

        var programmes = await db.Programmes
            .Where(p => p.PartnerId == partnerId && p.DeletedAt == null)
            .Select(p => new
            {
                p.ProgrammeId,
                p.Name,
                p.Code,
                p.Status,
                p.IsActive,
                p.IsDisabledByAdmin,
                p.RejectionReason,
                p.ClonedFromProgrammeId,
                MajorCount = p.Majors.Count(m => m.DeletedAt == null),
                SubjectCount = p.Majors
                    .Where(m => m.DeletedAt == null)
                    .SelectMany(m => m.Subjects)
                    .Count(s => s.DeletedAt == null),
            })
            .ToListAsync(ct);

        var programmeIds = programmes.Select(p => p.ProgrammeId).ToList();
        var enrolledIds = await db.StudentEnrollments
            .IgnoreQueryFilters()
            .Where(e => programmeIds.Contains(e.ProgrammeId))
            .Select(e => e.ProgrammeId)
            .Distinct()
            .ToListAsync(ct);
        var enrolledSet = enrolledIds.ToHashSet();

        var pathwayLinks = await db.ProgrammePathways
            .Where(pp => programmeIds.Contains(pp.ProgrammeId) && pp.DeletedAt == null)
            .Select(pp => new { pp.ProgrammeId, pp.PathwayId })
            .ToListAsync(ct);
        var pathwaysByProgramme = pathwayLinks
            .GroupBy(pp => pp.ProgrammeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.PathwayId).ToList());

        var items = programmes
            .OrderBy(p => p.Name)
            .Select(p => new PartnerV1MyProgramsListItem
            {
                ProgrammeId = p.ProgrammeId,
                Name = p.Name,
                Code = p.Code,
                Status = p.Status.ToString(),
                IsActive = p.IsActive,
                IsDisabledByAdmin = p.IsDisabledByAdmin,
                RejectionReason = p.RejectionReason,
                ClonedFromProgrammeId = p.ClonedFromProgrammeId,
                MajorCount = p.MajorCount,
                SubjectCount = p.SubjectCount,
                HasEnrolments = enrolledSet.Contains(p.ProgrammeId),
                CanDelete = !enrolledSet.Contains(p.ProgrammeId) && !p.IsDisabledByAdmin,
                PathwayIds = pathwaysByProgramme.TryGetValue(p.ProgrammeId, out var pids) ? pids : [],
            })
            .ToList();

        return Results.Ok(new PartnerV1MyProgramsListResponse { Items = items, Links = [] });
    }

    private const string Route = "/v1/partner/my-programs";
}

public sealed class PartnerV1MyProgramsListResponse : HateoasBaseResponse
{
    public required IReadOnlyList<PartnerV1MyProgramsListItem> Items { get; init; }
}

public sealed class PartnerV1MyProgramsListItem
{
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required string Status { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsDisabledByAdmin { get; init; }
    public string? RejectionReason { get; init; }
    public Guid? ClonedFromProgrammeId { get; init; }
    public required int MajorCount { get; init; }
    public required int SubjectCount { get; init; }
    public required bool HasEnrolments { get; init; }
    public required bool CanDelete { get; init; }
    public required IReadOnlyList<int> PathwayIds { get; init; }
}

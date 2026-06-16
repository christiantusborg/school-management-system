using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.Programmes.Endpoint;

/// <summary>
/// Anonymous public catalogue: returns IBSS core programmes
/// (those without a partner owner) with their specializations and subjects.
/// Used by the public Programmes browse page and the signup wizard.
/// </summary>
[Route("/v1/public/programmes")]
[EndpointTag("Public.Programmes")]
public sealed class PublicProgrammesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/programmes", HandleAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        OdinDbContext db,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null)
    {
        var programmesQuery = db.Programmes
            .Where(p => p.DeletedAt == null && p.OwnerId == null);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q.Trim()}%";
            programmesQuery = programmesQuery.Where(p =>
                EF.Functions.ILike(p.Code, like) ||
                EF.Functions.ILike(p.Name, like) ||
                EF.Functions.ILike(p.Description, like));
        }

        var programmes = await programmesQuery
            .OrderBy(p => p.Code)
            .Select(p => new
            {
                p.ProgrammeId,
                p.Code,
                p.Name,
                p.Description,
            })
            .ToListAsync(cancellationToken);

        if (programmes.Count == 0)
            return Results.Ok(new PublicProgrammesV1ListResponse { Items = [], Total = 0 });

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
            .ToListAsync(cancellationToken);

        var specIds = specs.Select(s => s.SpecializationId).ToList();
        var subjects = await db.Subjects
            .Where(s => s.DeletedAt == null && specIds.Contains(s.SpecializationId))
            .OrderBy(s => s.Code)
            .Select(s => new
            {
                s.SubjectId,
                s.SpecializationId,
                s.Code,
                s.Name,
                s.Ects,
            })
            .ToListAsync(cancellationToken);

        var subjectsBySpec = subjects
            .GroupBy(s => s.SpecializationId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var specsByProgramme = specs
            .GroupBy(s => s.ProgrammeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = programmes.Select(p => new PublicProgrammeItem
        {
            ProgrammeId = p.ProgrammeId,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            Specializations = specsByProgramme.TryGetValue(p.ProgrammeId, out var sps)
                ? sps.Select(s => new PublicSpecializationItem
                  {
                      SpecializationId = s.SpecializationId,
                      Code = s.Code,
                      Name = s.Name,
                      Description = s.Description,
                      DurationOfStudyMonths = s.DurationOfStudyMonths,
                      Subjects = subjectsBySpec.TryGetValue(s.SpecializationId, out var subs)
                          ? subs.Select(sb => new PublicSubjectItem
                            {
                                SubjectId = sb.SubjectId,
                                Code = sb.Code,
                                Name = sb.Name,
                                Ects = sb.Ects,
                            }).ToList()
                          : new List<PublicSubjectItem>(),
                  }).ToList()
                : new List<PublicSpecializationItem>(),
        }).ToList();

        return Results.Ok(new PublicProgrammesV1ListResponse { Items = items, Total = items.Count });
    }
}

public sealed class PublicProgrammesV1ListResponse
{
    public required IReadOnlyList<PublicProgrammeItem> Items { get; init; }
    public required int Total { get; init; }
}

public sealed class PublicProgrammeItem
{
    public required Guid ProgrammeId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<PublicSpecializationItem> Specializations { get; init; }
}

public sealed class PublicSpecializationItem
{
    public required Guid SpecializationId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int DurationOfStudyMonths { get; init; }
    public required IReadOnlyList<PublicSubjectItem> Subjects { get; init; }
}

public sealed class PublicSubjectItem
{
    public required Guid SubjectId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required decimal Ects { get; init; }
}

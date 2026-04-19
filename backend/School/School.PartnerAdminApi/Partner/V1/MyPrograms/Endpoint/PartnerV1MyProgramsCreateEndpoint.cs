using System.Security.Claims;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsCreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        [FromBody] PartnerV1MyProgramsCreateRequest request,
        [FromServices] OdinDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user?.PartnerId is null) return Results.Forbid();
        var partnerId = user.PartnerId.Value;

        var partner = await db.Partners.FirstOrDefaultAsync(p => p.PartnerId == partnerId, ct);
        if (partner is null) return Results.Forbid();

        var prefix = MyProgramsHelpers.BuildPartnerPrefix(partner.Name);
        var newProgrammeId = Guid.NewGuid();

        var requestedPathwayIds = request.PathwayIds is null
            ? null
            : request.PathwayIds.Where(id => id > 0).Distinct().ToList();
        if (requestedPathwayIds is { Count: > 0 } &&
            !await MyProgramsHelpers.ValidatePathwayIdsAsync(db, requestedPathwayIds, ct))
        {
            return Results.BadRequest(new { error = "invalid_pathway_ids" });
        }

        if (request.SourceProgrammeId is Guid sourceId)
        {
            var source = await db.Programmes
                .FirstOrDefaultAsync(p => p.ProgrammeId == sourceId
                                       && p.PartnerId == null
                                       && p.DeletedAt == null, ct);
            if (source is null) return Results.BadRequest(new { error = "source_programme_not_found" });

            var hasAnyAccess = await db.PartnerProgrammeAccesses
                .AnyAsync(a => a.PartnerId == partnerId
                            && a.ProgrammeId == sourceId
                            && a.DeletedAt == null, ct);
            if (!hasAnyAccess) return Results.Forbid();

            var sourceMajors = await db.Majors
                .Where(m => m.ProgrammeId == sourceId && m.DeletedAt == null)
                .Include(m => m.Subjects.Where(s => s.DeletedAt == null))
                .ToListAsync(ct);

            var code = await MyProgramsHelpers.GenerateUniqueCodeAsync(db, prefix, source.Code, ct);

            var clone = new Programme
            {
                ProgrammeId = newProgrammeId,
                Name = source.Name,
                Code = code,
                PartnerId = partnerId,
                ClonedFromProgrammeId = sourceId,
                Status = ProgrammeStatus.Draft,
                IsActive = false,
                CreatedByUserId = userId,
                Majors = sourceMajors.Select(m => new Major
                {
                    MajorId = Guid.NewGuid(),
                    Name = m.Name,
                    Subjects = m.Subjects.Select(s => new Subject
                    {
                        SubjectId = Guid.NewGuid(),
                        Code = s.Code,
                        Name = s.Name,
                        Ects = s.Ects,
                    }).ToList(),
                }).ToList(),
            };
            db.Programmes.Add(clone);

            var pathwayIdsForClone = requestedPathwayIds
                ?? await MyProgramsHelpers.GetCurrentPathwayIdsAsync(db, sourceId, ct);
            foreach (var pathwayId in pathwayIdsForClone)
            {
                db.ProgrammePathways.Add(new ProgrammePathway
                {
                    ProgrammeId = newProgrammeId,
                    PathwayId = pathwayId,
                });
            }
        }
        else
        {
            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return Results.BadRequest(new { error = "name_required" });

            var baseCode = $"NEW-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var code = await MyProgramsHelpers.GenerateUniqueCodeAsync(db, prefix, baseCode, ct);

            var fresh = new Programme
            {
                ProgrammeId = newProgrammeId,
                Name = name,
                Code = code,
                PartnerId = partnerId,
                ClonedFromProgrammeId = null,
                Status = ProgrammeStatus.Draft,
                IsActive = false,
                CreatedByUserId = userId,
            };
            db.Programmes.Add(fresh);

            foreach (var pathwayId in requestedPathwayIds ?? [])
            {
                db.ProgrammePathways.Add(new ProgrammePathway
                {
                    ProgrammeId = newProgrammeId,
                    PathwayId = pathwayId,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Created($"/v1/partner/my-programs/{newProgrammeId}", new { programmeId = newProgrammeId });
    }

    private const string Route = "/v1/partner/my-programs";
}

public sealed class PartnerV1MyProgramsCreateRequest
{
    public Guid? SourceProgrammeId { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<int>? PathwayIds { get; init; }
}

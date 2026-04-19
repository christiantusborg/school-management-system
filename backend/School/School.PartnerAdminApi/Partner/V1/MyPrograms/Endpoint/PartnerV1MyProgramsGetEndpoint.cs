namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs/{id:guid}")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsGetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, EndpointHandlerAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] OdinDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var (_, partnerIdOrNull) = await MyProgramsHelpers.ResolvePartnerAsync(httpContext, db, ct);
        if (partnerIdOrNull is null) return Results.Forbid();
        var partnerId = partnerIdOrNull.Value;

        var programme = await db.Programmes
            .FirstOrDefaultAsync(p => p.ProgrammeId == id
                                   && p.PartnerId == partnerId
                                   && p.DeletedAt == null, ct);
        if (programme is null) return Results.NotFound();

        var majors = await db.Majors
            .Where(m => m.ProgrammeId == id && m.DeletedAt == null)
            .OrderBy(m => m.Name)
            .Select(m => new PartnerV1MyProgramsGetMajor
            {
                MajorId = m.MajorId,
                Name = m.Name,
                Subjects = m.Subjects
                    .Where(s => s.DeletedAt == null)
                    .OrderBy(s => s.Code)
                    .Select(s => new PartnerV1MyProgramsGetSubject
                    {
                        SubjectId = s.SubjectId,
                        Code = s.Code,
                        Name = s.Name,
                        Ects = s.Ects,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);

        var hasEnrolments = await MyProgramsHelpers.HasEnrolmentsEverAsync(db, id, ct);
        var pathwayIds = await MyProgramsHelpers.GetCurrentPathwayIdsAsync(db, id, ct);

        return Results.Ok(new PartnerV1MyProgramsGetResponse
        {
            ProgrammeId = programme.ProgrammeId,
            Name = programme.Name,
            Code = programme.Code,
            Status = programme.Status.ToString(),
            IsActive = programme.IsActive,
            IsDisabledByAdmin = programme.IsDisabledByAdmin,
            RejectionReason = programme.RejectionReason,
            ClonedFromProgrammeId = programme.ClonedFromProgrammeId,
            HasEnrolments = hasEnrolments,
            Majors = majors,
            PathwayIds = pathwayIds,
            Links = [],
        });
    }

    private const string Route = "/v1/partner/my-programs/{id:guid}";
}

public sealed class PartnerV1MyProgramsGetResponse : HateoasBaseResponse
{
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required string Status { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsDisabledByAdmin { get; init; }
    public string? RejectionReason { get; init; }
    public Guid? ClonedFromProgrammeId { get; init; }
    public required bool HasEnrolments { get; init; }
    public required IReadOnlyList<PartnerV1MyProgramsGetMajor> Majors { get; init; }
    public required IReadOnlyList<int> PathwayIds { get; init; }
}

public sealed class PartnerV1MyProgramsGetMajor
{
    public required Guid MajorId { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<PartnerV1MyProgramsGetSubject> Subjects { get; init; }
}

public sealed class PartnerV1MyProgramsGetSubject
{
    public required Guid SubjectId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int Ects { get; init; }
}

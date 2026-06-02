namespace School.ProgrammeApi;

/// <summary>
/// Programme single-row CRUD: Create, Get, Update (incl. pathway assignment),
/// SoftDelete, Restore, PermanentDelete. The List endpoint lives in its own
/// marker file (`SchoolProgrammesV1ListEndpoint`).
/// </summary>
[Route("/v1/school/programmes")]
[EndpointTag("School.Programmes")]
public sealed class SchoolProgrammesV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/school/programmes",                      CreateAsync).RequireAuthorization();
        app.MapGet("/v1/school/programmes/{id:guid}",             GetAsync).RequireAuthorization();
        app.MapPut("/v1/school/programmes/{id:guid}",             UpdateAsync).RequireAuthorization();
        app.MapDelete("/v1/school/programmes/{id:guid}",          SoftDeleteAsync).RequireAuthorization();
        app.MapPost("/v1/school/programmes/{id:guid}/restore",    RestoreAsync).RequireAuthorization();
        app.MapDelete("/v1/school/programmes/{id:guid}/permanent", PermanentDeleteAsync).RequireAuthorization();
        return app;
    }

    public sealed class WriteRequest
    {
        public string? Name { get; init; }
        public string? Code { get; init; }
        public string? Description { get; init; }
        public IReadOnlyList<Guid>? PathwayIds { get; init; }
        public Guid? AwardEducationLevelId { get; init; }
        /// <summary>
        /// Allowed duration range in months. Both bounds required on create.
        /// On update, omit either bound to leave it unchanged; supplying both
        /// must satisfy 1 ≤ min ≤ max.
        /// </summary>
        public int? MinDurationMonths { get; init; }
        public int? MaxDurationMonths { get; init; }
        /// <summary>
        /// Partner the programme belongs to. null = core (admin-shared) programme.
        /// </summary>
        public Guid? OwnerPartnerId { get; init; }
    }

    private static IResult? ValidateDurationRange(int? min, int? max)
    {
        if (min is null || max is null)
            return Results.BadRequest(new { message = "minDurationMonths and maxDurationMonths are required" });
        if (min < 1)
            return Results.BadRequest(new { message = "minDurationMonths must be at least 1" });
        if (max < min)
            return Results.BadRequest(new { message = "maxDurationMonths must be greater than or equal to minDurationMonths" });
        return null;
    }

    private static async Task<IResult> CreateAsync(
        OdinDbContext db, [FromBody] WriteRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Code))
            return Results.BadRequest(new { message = "name and code are required" });
        if (ValidateDurationRange(body.MinDurationMonths, body.MaxDurationMonths) is { } durErr)
            return durErr;

        var entity = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.Programme
        {
            ProgrammeId = Guid.NewGuid(),
            Name = body.Name.Trim(),
            Code = body.Code.Trim().ToUpperInvariant(),
            Description = body.Description ?? string.Empty,
            OwnerId = body.OwnerPartnerId,
            AwardEducationLevelId = body.AwardEducationLevelId,
            MinDurationMonths = body.MinDurationMonths!.Value,
            MaxDurationMonths = body.MaxDurationMonths!.Value,
        };
        db.Programmes.Add(entity);

        foreach (var pathwayId in (body.PathwayIds ?? Array.Empty<Guid>()).Distinct())
        {
            db.ProgrammePathways.Add(new ProgrammePathway
            {
                ProgrammePathwayId = Guid.NewGuid(),
                ProgrammeId = entity.ProgrammeId,
                PathwayId = pathwayId,
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.Created($"/v1/school/programmes/{entity.ProgrammeId}", new { programmeId = entity.ProgrammeId });
    }

    private static async Task<IResult> GetAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var prog = await db.Programmes
            .Where(p => p.ProgrammeId == id)
            .Select(p => new { p.ProgrammeId, p.Code, p.Name, p.Description, p.OwnerId, p.AwardEducationLevelId, p.MinDurationMonths, p.MaxDurationMonths, p.DeletedAt })
            .FirstOrDefaultAsync(ct);
        if (prog is null) return Results.NotFound();

        var pathwayIds = await db.ProgrammePathways
            .Where(pp => pp.ProgrammeId == id && pp.DeletedAt == null)
            .Select(pp => pp.PathwayId)
            .ToListAsync(ct);

        return Results.Ok(new
        {
            programmeId = prog.ProgrammeId,
            code = prog.Code,
            name = prog.Name,
            description = prog.Description,
            ownerId = prog.OwnerId,
            awardEducationLevelId = prog.AwardEducationLevelId,
            minDurationMonths = prog.MinDurationMonths,
            maxDurationMonths = prog.MaxDurationMonths,
            deletedAt = prog.DeletedAt,
            pathwayIds,
        });
    }

    private static async Task<IResult> UpdateAsync(
        OdinDbContext db, Guid id, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var prog = await db.Programmes.FirstOrDefaultAsync(p => p.ProgrammeId == id, ct);
        if (prog is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(body.Name)) prog.Name = body.Name.Trim();
        if (!string.IsNullOrWhiteSpace(body.Code)) prog.Code = body.Code.Trim().ToUpperInvariant();
        if (body.Description is not null)          prog.Description = body.Description;
        // Allow explicit clear-to-null on the award level (PATCH-style behaviour
        // would be cleaner, but PUT here accepts null as "no award").
        prog.AwardEducationLevelId = body.AwardEducationLevelId;

        // Duration range: each bound is independently optional on update, but
        // the final pair must still satisfy 1 ≤ min ≤ max.
        var newMin = body.MinDurationMonths ?? prog.MinDurationMonths;
        var newMax = body.MaxDurationMonths ?? prog.MaxDurationMonths;
        if (ValidateDurationRange(newMin, newMax) is { } durErr) return durErr;
        prog.MinDurationMonths = newMin;
        prog.MaxDurationMonths = newMax;

        if (body.PathwayIds is not null)
        {
            // Replace pathway links — hard-delete old, insert new (avoids the
            // unique index collision that soft-delete would cause on re-edit).
            var existing = await db.ProgrammePathways.Where(pp => pp.ProgrammeId == id).ToListAsync(ct);
            db.ProgrammePathways.RemoveRange(existing);
            foreach (var pathwayId in body.PathwayIds.Distinct())
            {
                db.ProgrammePathways.Add(new ProgrammePathway
                {
                    ProgrammePathwayId = Guid.NewGuid(),
                    ProgrammeId = id,
                    PathwayId = pathwayId,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId = id });
    }

    private static async Task<IResult> SoftDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var prog = await db.Programmes.FirstOrDefaultAsync(p => p.ProgrammeId == id && p.DeletedAt == null, ct);
        if (prog is null) return Results.NotFound();
        prog.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId = id });
    }

    private static async Task<IResult> RestoreAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var prog = await db.Programmes.FirstOrDefaultAsync(p => p.ProgrammeId == id, ct);
        if (prog is null) return Results.NotFound();
        prog.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId = id });
    }

    private static async Task<IResult> PermanentDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var prog = await db.Programmes.FirstOrDefaultAsync(p => p.ProgrammeId == id, ct);
        if (prog is null) return Results.NotFound();
        // Cascade pathway-link rows + specializations; the EF configurations
        // already declare ON DELETE CASCADE for those FKs.
        db.Programmes.Remove(prog);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId = id });
    }
}

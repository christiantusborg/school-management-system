namespace School.SpecializationApi;

[Route("/v1/school/specializations")]
[EndpointTag("School.Specializations")]
public sealed class SchoolSpecializationsV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/school/specializations",                       CreateAsync).RequireAuthorization();
        app.MapGet("/v1/school/specializations/{id:guid}",              GetAsync).RequireAuthorization();
        app.MapPut("/v1/school/specializations/{id:guid}",              UpdateAsync).RequireAuthorization();
        app.MapDelete("/v1/school/specializations/{id:guid}",           SoftDeleteAsync).RequireAuthorization();
        app.MapPost("/v1/school/specializations/{id:guid}/restore",     RestoreAsync).RequireAuthorization();
        app.MapDelete("/v1/school/specializations/{id:guid}/permanent", PermanentDeleteAsync).RequireAuthorization();
        return app;
    }

    public sealed class WriteRequest
    {
        public Guid? ProgrammeId { get; init; }
        public string? Code { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public int? DurationOfStudyMonths { get; init; }
        public string? OfferAcceptanceMode { get; init; }
        public string? InstructionLanguage { get; init; }
    }

    private static OfferAcceptanceMode? ParseOfferAcceptanceMode(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? null
            : Enum.TryParse<OfferAcceptanceMode>(raw, ignoreCase: true, out var v) ? v : (OfferAcceptanceMode?)null;

    private static async Task<IResult> CreateAsync(
        OdinDbContext db, [FromBody] WriteRequest body, CancellationToken ct)
    {
        if (body.ProgrammeId is null || string.IsNullOrWhiteSpace(body.Name))
            return Results.BadRequest(new { message = "programmeId and name are required" });

        var entity = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.Specialization
        {
            SpecializationId = Guid.NewGuid(),
            ProgrammeId = body.ProgrammeId.Value,
            Name = body.Name.Trim(),
            // Code is optional; default to a slug of the name so the unique
            // (ProgrammeId, Code) index has something to bite on.
            Code = string.IsNullOrWhiteSpace(body.Code)
                ? body.Name.Trim().ToUpperInvariant().Replace(' ', '-')
                : body.Code.Trim().ToUpperInvariant(),
            Description = body.Description ?? string.Empty,
            DurationOfStudyMonths = body.DurationOfStudyMonths ?? 12,
            OfferAcceptanceMode = ParseOfferAcceptanceMode(body.OfferAcceptanceMode) ?? OfferAcceptanceMode.StudentAccept,
            InstructionLanguage = string.IsNullOrWhiteSpace(body.InstructionLanguage) ? null : body.InstructionLanguage.Trim(),
            IsActive = DateTime.UtcNow,
        };
        db.Specializations.Add(entity);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/v1/school/specializations/{entity.SpecializationId}",
            new { specializationId = entity.SpecializationId });
    }

    private static async Task<IResult> GetAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var spec = await db.Specializations
            .Where(s => s.SpecializationId == id)
            .Select(s => new
            {
                specializationId = s.SpecializationId,
                programmeId = s.ProgrammeId,
                code = s.Code,
                name = s.Name,
                description = s.Description,
                durationOfStudyMonths = s.DurationOfStudyMonths,
                offerAcceptanceMode = s.OfferAcceptanceMode.ToString(),
                instructionLanguage = s.InstructionLanguage,
                deletedAt = s.DeletedAt,
            })
            .FirstOrDefaultAsync(ct);
        return spec is null ? Results.NotFound() : Results.Ok(spec);
    }

    private static async Task<IResult> UpdateAsync(
        OdinDbContext db, Guid id, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var entity = await db.Specializations.FirstOrDefaultAsync(s => s.SpecializationId == id, ct);
        if (entity is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(body.Name)) entity.Name = body.Name.Trim();
        if (!string.IsNullOrWhiteSpace(body.Code)) entity.Code = body.Code.Trim().ToUpperInvariant();
        if (body.Description is not null) entity.Description = body.Description;
        if (body.DurationOfStudyMonths is not null) entity.DurationOfStudyMonths = body.DurationOfStudyMonths.Value;
        var parsedMode = ParseOfferAcceptanceMode(body.OfferAcceptanceMode);
        if (parsedMode is not null) entity.OfferAcceptanceMode = parsedMode.Value;
        if (body.InstructionLanguage is not null)
            entity.InstructionLanguage = string.IsNullOrWhiteSpace(body.InstructionLanguage) ? null : body.InstructionLanguage.Trim();

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { specializationId = id });
    }

    private static async Task<IResult> SoftDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.Specializations.FirstOrDefaultAsync(s => s.SpecializationId == id && s.DeletedAt == null, ct);
        if (entity is null) return Results.NotFound();
        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { specializationId = id });
    }

    private static async Task<IResult> RestoreAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.Specializations.FirstOrDefaultAsync(s => s.SpecializationId == id, ct);
        if (entity is null) return Results.NotFound();
        entity.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { specializationId = id });
    }

    private static async Task<IResult> PermanentDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.Specializations.FirstOrDefaultAsync(s => s.SpecializationId == id, ct);
        if (entity is null) return Results.NotFound();
        db.Specializations.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { specializationId = id });
    }
}

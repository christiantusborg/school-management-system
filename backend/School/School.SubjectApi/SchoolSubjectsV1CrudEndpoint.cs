namespace School.SubjectApi;

[Route("/v1/school/subjects")]
[EndpointTag("School.Subjects")]
public sealed class SchoolSubjectsV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/school/subjects",                       CreateAsync).RequireAuthorization();
        app.MapGet("/v1/school/subjects/{id:guid}",              GetAsync).RequireAuthorization();
        app.MapPut("/v1/school/subjects/{id:guid}",              UpdateAsync).RequireAuthorization();
        app.MapDelete("/v1/school/subjects/{id:guid}",           SoftDeleteAsync).RequireAuthorization();
        app.MapPost("/v1/school/subjects/{id:guid}/restore",     RestoreAsync).RequireAuthorization();
        app.MapDelete("/v1/school/subjects/{id:guid}/permanent", PermanentDeleteAsync).RequireAuthorization();
        app.MapPut("/v1/school/subjects/{id:guid}/thesis",       SetThesisAsync).RequireAuthorization();
        return app;
    }

    /// <summary>
    /// Marks this subject as THE thesis of its specialization, atomically
    /// clearing the flag on every sibling — a specialization has at most one
    /// thesis subject (radio-button semantics in the admin drawer).
    /// </summary>
    private static async Task<IResult> SetThesisAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id && s.DeletedAt == null, ct);
        if (subject is null) return Results.NotFound();

        var siblings = await db.Subjects
            .Where(s => s.SpecializationId == subject.SpecializationId && s.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var s in siblings) s.IsThesis = s.SubjectId == id;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { subjectId = id, specializationId = subject.SpecializationId, isThesis = true });
    }

    public sealed class WriteRequest
    {
        public Guid? SpecializationId { get; init; }
        public string? Code { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public decimal? Ects { get; init; }
        public bool? IsThesis { get; init; }
    }

    private static async Task<IResult> CreateAsync(
        OdinDbContext db, [FromBody] WriteRequest body, CancellationToken ct)
    {
        if (body.SpecializationId is null || string.IsNullOrWhiteSpace(body.Name))
            return Results.BadRequest(new { message = "specializationId and name are required" });

        var entity = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.Subject
        {
            SubjectId = Guid.NewGuid(),
            SpecializationId = body.SpecializationId.Value,
            Name = body.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(body.Code)
                ? body.Name.Trim().ToUpperInvariant().Replace(' ', '-')
                : body.Code.Trim().ToUpperInvariant(),
            Description = body.Description ?? string.Empty,
            Ects = body.Ects ?? 0,
            IsThesis = body.IsThesis ?? false,
            IsActive = DateTime.UtcNow,
        };
        db.Subjects.Add(entity);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/v1/school/subjects/{entity.SubjectId}", new { subjectId = entity.SubjectId });
    }

    private static async Task<IResult> GetAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var s = await db.Subjects
            .Where(x => x.SubjectId == id)
            .Select(x => new
            {
                subjectId = x.SubjectId,
                specializationId = x.SpecializationId,
                code = x.Code,
                name = x.Name,
                description = x.Description,
                ects = x.Ects,
                isThesis = x.IsThesis,
                deletedAt = x.DeletedAt,
            })
            .FirstOrDefaultAsync(ct);
        return s is null ? Results.NotFound() : Results.Ok(s);
    }

    private static async Task<IResult> UpdateAsync(
        OdinDbContext db, Guid id, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var entity = await db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id, ct);
        if (entity is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(body.Name)) entity.Name = body.Name.Trim();
        if (!string.IsNullOrWhiteSpace(body.Code)) entity.Code = body.Code.Trim().ToUpperInvariant();
        if (body.Description is not null) entity.Description = body.Description;
        if (body.Ects is not null) entity.Ects = body.Ects.Value;
        if (body.IsThesis is not null) entity.IsThesis = body.IsThesis.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { subjectId = id });
    }

    private static async Task<IResult> SoftDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id && s.DeletedAt == null, ct);
        if (entity is null) return Results.NotFound();
        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { subjectId = id });
    }

    private static async Task<IResult> RestoreAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id, ct);
        if (entity is null) return Results.NotFound();
        entity.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { subjectId = id });
    }

    private static async Task<IResult> PermanentDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id, ct);
        if (entity is null) return Results.NotFound();
        db.Subjects.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { subjectId = id });
    }
}

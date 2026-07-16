using SchoolEntity = SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.School;

namespace School.ProgrammeApi;

/// <summary>
/// CRUD for awarding schools (System Config → Schools). List (optionally
/// including soft-deleted), Create, Update, SoftDelete (blocked while any
/// programme still references the school), Restore, and a lightweight Options
/// endpoint for the programme create/edit dropdowns.
/// </summary>
[Route("/v1/school/schools")]
[EndpointTag("School.Schools")]
public sealed class SchoolsV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/school/schools", ListAsync).RequireAuthorization();
        app.MapGet("/v1/school/schools/options", OptionsAsync).RequireAuthorization();
        app.MapPost("/v1/school/schools", CreateAsync).RequireAuthorization();
        app.MapPut("/v1/school/schools/{id:guid}", UpdateAsync).RequireAuthorization();
        app.MapDelete("/v1/school/schools/{id:guid}", SoftDeleteAsync).RequireAuthorization();
        app.MapPost("/v1/school/schools/{id:guid}/restore", RestoreAsync).RequireAuthorization();
        return app;
    }

    public sealed class WriteRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
    }

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.Schools
            .Where(s => includeDeleted || s.DeletedAt == null)
            .OrderBy(s => s.DeletedAt != null) // active first
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                schoolId = s.SchoolId,
                name = s.Name,
                description = s.Description,
                deletedAt = s.DeletedAt,
                programmeCount = db.Programmes.Count(p => p.SchoolId == s.SchoolId && p.DeletedAt == null),
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> OptionsAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.Schools
            .Where(s => s.DeletedAt == null)
            .OrderBy(s => s.Name)
            .Select(s => new { schoolId = s.SchoolId, name = s.Name })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> CreateAsync(OdinDbContext db, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "name is required" });

        var entity = new SchoolEntity
        {
            SchoolId = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(body.Description) ? null : body.Description.Trim(),
        };
        db.Schools.Add(entity);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/v1/school/schools/{entity.SchoolId}", new { schoolId = entity.SchoolId });
    }

    private static async Task<IResult> UpdateAsync(OdinDbContext db, Guid id, [FromBody] WriteRequest body, CancellationToken ct)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.SchoolId == id, ct);
        if (school is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(body.Name)) school.Name = body.Name.Trim();
        school.Description = string.IsNullOrWhiteSpace(body.Description) ? null : body.Description.Trim();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { schoolId = id });
    }

    private static async Task<IResult> SoftDeleteAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.SchoolId == id && s.DeletedAt == null, ct);
        if (school is null) return Results.NotFound();

        var inUse = await db.Programmes.CountAsync(p => p.SchoolId == id && p.DeletedAt == null, ct);
        if (inUse > 0)
            return Results.BadRequest(new
            {
                message = $"Cannot delete: {inUse} programme(s) still use this school. Reassign them to another school first."
            });

        school.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { schoolId = id });
    }

    private static async Task<IResult> RestoreAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.SchoolId == id, ct);
        if (school is null) return Results.NotFound();
        school.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { schoolId = id });
    }
}

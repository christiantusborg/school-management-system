namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Multiple Student IDs per student. One is primary (mirrored into
/// Student.StudentNumber — what lists and letters print); the rest are
/// aliases with an optional origin label. Values are globally unique across
/// all students. Admission-only writes; partners and students read the list
/// via their detail endpoints. Switching primary does NOT re-render already
/// released PDFs (explicitly decided).
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/identifiers")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsIdentifiersEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/identifiers", AddAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/students/{studentId:guid}/identifiers/{id:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/students/{studentId:guid}/identifiers/{id:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/students/{studentId:guid}/identifiers/{id:guid}/make-primary", MakePrimaryAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class Body { public string? Value { get; init; } public string? Label { get; init; } }

    private static async Task<IResult> ListAsync(OdinDbContext db, Guid studentId, CancellationToken ct)
    {
        var items = await db.StudentIdentifiers
            .Where(i => i.StudentId == studentId)
            .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.CreatedAt)
            .Select(i => new { studentIdentifierId = i.StudentIdentifierId, value = i.Value, label = i.Label, isPrimary = i.IsPrimary })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<bool> TakenAsync(OdinDbContext db, string value, Guid? exceptId, CancellationToken ct) =>
        await db.StudentIdentifiers.AnyAsync(i => i.Value.ToLower() == value.ToLower()
            && (exceptId == null || i.StudentIdentifierId != exceptId), ct)
        || await db.Students.AnyAsync(s => s.DeletedAt == null && s.StudentNumber.ToLower() == value.ToLower()
            && !db.StudentIdentifiers.Any(i => i.StudentId == s.StudentId && i.Value.ToLower() == value.ToLower()), ct);

    private static async Task<IResult> AddAsync(
        Guid studentId, [FromBody] Body body, OdinDbContext db, CancellationToken ct)
    {
        var value = body.Value?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return Results.BadRequest(new { error = "Value is required." });
        if (!await db.Students.AnyAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct))
            return Results.NotFound();
        if (await TakenAsync(db, value, null, ct))
            return Results.BadRequest(new { error = $"Student ID '{value}' already belongs to a student." });
        db.StudentIdentifiers.Add(new SharedLibrary.Basics.Opaque.Domains.StudentIdentifier
        {
            StudentId = studentId,
            Value = value,
            Label = string.IsNullOrWhiteSpace(body.Label) ? null : body.Label.Trim(),
            IsPrimary = false,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return await ListAsync(db, studentId, ct);
    }

    private static async Task<IResult> UpdateAsync(
        Guid studentId, Guid id, [FromBody] Body body, OdinDbContext db, CancellationToken ct)
    {
        var row = await db.StudentIdentifiers.FirstOrDefaultAsync(i => i.StudentIdentifierId == id && i.StudentId == studentId, ct);
        if (row is null) return Results.NotFound();
        var value = body.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(value) && !string.Equals(value, row.Value, StringComparison.Ordinal))
        {
            if (await TakenAsync(db, value, id, ct))
                return Results.BadRequest(new { error = $"Student ID '{value}' already belongs to a student." });
            row.Value = value;
            // Primary edits follow through to the denormalized display number.
            if (row.IsPrimary)
            {
                var student = await db.Students.FirstAsync(s => s.StudentId == studentId, ct);
                student.StudentNumber = value;
            }
        }
        row.Label = string.IsNullOrWhiteSpace(body.Label) ? null : body.Label.Trim();
        await db.SaveChangesAsync(ct);
        return await ListAsync(db, studentId, ct);
    }

    private static async Task<IResult> DeleteAsync(
        Guid studentId, Guid id, OdinDbContext db, CancellationToken ct)
    {
        var row = await db.StudentIdentifiers.FirstOrDefaultAsync(i => i.StudentIdentifierId == id && i.StudentId == studentId, ct);
        if (row is null) return Results.NotFound();
        if (row.IsPrimary) return Results.BadRequest(new { error = "The primary Student ID cannot be removed — make another ID primary first." });
        db.StudentIdentifiers.Remove(row);
        await db.SaveChangesAsync(ct);
        return await ListAsync(db, studentId, ct);
    }

    private static async Task<IResult> MakePrimaryAsync(
        Guid studentId, Guid id, OdinDbContext db, CancellationToken ct)
    {
        var row = await db.StudentIdentifiers.FirstOrDefaultAsync(i => i.StudentIdentifierId == id && i.StudentId == studentId, ct);
        if (row is null) return Results.NotFound();
        foreach (var other in await db.StudentIdentifiers.Where(i => i.StudentId == studentId && i.IsPrimary).ToListAsync(ct))
            other.IsPrimary = false;
        row.IsPrimary = true;
        var student = await db.Students.FirstAsync(s => s.StudentId == studentId, ct);
        student.StudentNumber = row.Value;
        await db.SaveChangesAsync(ct);
        return await ListAsync(db, studentId, ct);
    }
}

using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Rubrics;

/// <summary>
/// Rubric-style grading setup. Shared rubric templates (System Config →
/// Grading Rubrics) plus the per-module rubric choice on the Academic page:
/// none (simple single grade), a shared template, or a custom rubric owned
/// by that module. Row weights must always total exactly 100.
/// </summary>
[Route("/v1/admin/rubric-templates")]
[EndpointTag("Admin.Rubrics")]
public sealed class AdminV1RubricsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/rubric-templates", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/rubric-templates", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/rubric-templates/{templateId:guid}", RenameAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/rubric-templates/{templateId:guid}/structure", SaveStructureAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/rubric-templates/{templateId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");

        // Per-module rubric choice, used by the Academic page (school scope,
        // guarded like the other /v1/school academic routes).
        app.MapGet("/v1/school/subjects/{subjectId:guid}/rubric", GetSubjectRubricAsync).RequireAuthorization();
        app.MapPut("/v1/school/subjects/{subjectId:guid}/rubric", SetSubjectRubricAsync).RequireAuthorization();
        return app;
    }

    public sealed class RowDto
    {
        public Guid? Id { get; init; }
        public string Section { get; init; } = string.Empty;
        public string Criteria { get; init; } = string.Empty;
        public int MaxPercent { get; init; }
    }

    private static string? ValidateRows(List<RowDto> rows)
    {
        if (rows.Count == 0) return "A rubric needs at least one row.";
        if (rows.Any(r => string.IsNullOrWhiteSpace(r.Section)))
            return "Every rubric row needs a Section.";
        if (rows.Any(r => r.MaxPercent is < 1 or > 100))
            return "Each row's Max % must be between 1 and 100.";
        var total = rows.Sum(r => r.MaxPercent);
        return total == 100 ? null : $"Row percentages must total exactly 100 (currently {total}).";
    }

    /// <summary>Reconciles a template's rows with the wanted list: incoming
    /// ids update in place, dropped rows soft-delete (saved student scores
    /// survive), and a new row matching a soft-deleted section+criteria
    /// restores it — the same pattern as the other builders.</summary>
    private static async Task ReconcileRowsAsync(OdinDbContext db, Guid templateId, List<RowDto> wanted, CancellationToken ct)
    {
        var existing = await db.RubricRows.Where(r => r.RubricTemplateId == templateId).ToListAsync(ct);
        var keep = new HashSet<Guid>();
        var sort = 0;
        foreach (var w in wanted)
        {
            var row = (w.Id is { } id ? existing.FirstOrDefault(r => r.RubricRowId == id) : null)
                ?? existing.FirstOrDefault(r => r.DeletedAt != null
                    && r.Section.Trim().Equals(w.Section.Trim(), StringComparison.OrdinalIgnoreCase)
                    && r.Criteria.Trim().Equals(w.Criteria.Trim(), StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                row = new RubricRow { RubricTemplateId = templateId };
                db.RubricRows.Add(row);
            }
            row.Section = w.Section.Trim();
            row.Criteria = w.Criteria.Trim();
            row.MaxPercent = w.MaxPercent;
            row.SortOrder = sort++;
            row.DeletedAt = null;
            keep.Add(row.RubricRowId);
        }
        foreach (var r in existing.Where(r => r.DeletedAt == null && !keep.Contains(r.RubricRowId)))
            r.DeletedAt = DateTime.UtcNow;
    }

    private static object RowsOf(List<RubricRow> rows) => rows
        .Where(r => r.DeletedAt == null)
        .OrderBy(r => r.SortOrder)
        .Select(r => new { id = r.RubricRowId, section = r.Section, criteria = r.Criteria, maxPercent = r.MaxPercent })
        .ToList();

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct)
    {
        var templates = await db.RubricTemplates
            .Where(t => t.IsShared && t.DeletedAt == null)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
        var ids = templates.Select(t => t.RubricTemplateId).ToList();
        var rows = await db.RubricRows.Where(r => ids.Contains(r.RubricTemplateId)).ToListAsync(ct);
        var usage = await db.Subjects
            .Where(s => s.DeletedAt == null && s.RubricTemplateId != null && ids.Contains(s.RubricTemplateId!.Value))
            .GroupBy(s => s.RubricTemplateId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        return Results.Ok(new
        {
            items = templates.Select(t => new
            {
                id = t.RubricTemplateId,
                name = t.Name,
                rows = RowsOf(rows.Where(r => r.RubricTemplateId == t.RubricTemplateId).ToList()),
                usedBy = usage.GetValueOrDefault(t.RubricTemplateId),
            }).ToList(),
        });
    }

    public sealed class NameBody { public string? Name { get; init; } }

    private static async Task<IResult> CreateAsync(OdinDbContext db, [FromBody] NameBody body, CancellationToken ct)
    {
        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(new { error = "Name is required." });
        var template = new RubricTemplate { Name = name, IsShared = true };
        db.RubricTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = template.RubricTemplateId, name = template.Name });
    }

    private static async Task<IResult> RenameAsync(OdinDbContext db, Guid templateId, [FromBody] NameBody body, CancellationToken ct)
    {
        var template = await db.RubricTemplates
            .FirstOrDefaultAsync(t => t.RubricTemplateId == templateId && t.IsShared && t.DeletedAt == null, ct);
        if (template is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(body.Name)) return Results.BadRequest(new { error = "Name is required." });
        template.Name = body.Name.Trim();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = templateId });
    }

    public sealed class StructureBody { public List<RowDto>? Rows { get; init; } }

    private static async Task<IResult> SaveStructureAsync(
        OdinDbContext db, Guid templateId, [FromBody] StructureBody body, CancellationToken ct)
    {
        var template = await db.RubricTemplates
            .FirstOrDefaultAsync(t => t.RubricTemplateId == templateId && t.IsShared && t.DeletedAt == null, ct);
        if (template is null) return Results.NotFound();
        var rows = body.Rows ?? [];
        if (ValidateRows(rows) is { } error) return Results.BadRequest(new { error });
        await ReconcileRowsAsync(db, templateId, rows, ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = templateId });
    }

    private static async Task<IResult> DeleteAsync(OdinDbContext db, Guid templateId, CancellationToken ct)
    {
        var template = await db.RubricTemplates
            .FirstOrDefaultAsync(t => t.RubricTemplateId == templateId && t.IsShared && t.DeletedAt == null, ct);
        if (template is null) return Results.NotFound();
        var inUse = await db.Subjects.CountAsync(s => s.RubricTemplateId == templateId && s.DeletedAt == null, ct);
        if (inUse > 0)
            return Results.Conflict(new { error = $"This rubric is used by {inUse} module(s) — remove it from them first." });
        template.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = templateId });
    }

    // ── Per-module rubric choice ────────────────────────────────────────────

    private static async Task<IResult> GetSubjectRubricAsync(OdinDbContext db, Guid subjectId, CancellationToken ct)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == subjectId && s.DeletedAt == null, ct);
        if (subject is null) return Results.NotFound();

        var sharedTemplates = await db.RubricTemplates
            .Where(t => t.IsShared && t.DeletedAt == null)
            .OrderBy(t => t.Name)
            .Select(t => new { id = t.RubricTemplateId, name = t.Name })
            .ToListAsync(ct);

        string mode = "none";
        object rows = new List<object>();
        if (subject.RubricTemplateId is { } tid)
        {
            var template = await db.RubricTemplates.FirstOrDefaultAsync(t => t.RubricTemplateId == tid, ct);
            if (template is not null)
            {
                mode = template.IsShared ? "shared" : "custom";
                rows = RowsOf(await db.RubricRows.Where(r => r.RubricTemplateId == tid).ToListAsync(ct));
            }
        }
        return Results.Ok(new { mode, templateId = subject.RubricTemplateId, rows, sharedTemplates });
    }

    public sealed class SubjectRubricBody
    {
        public string? Mode { get; init; }
        public Guid? TemplateId { get; init; }
        public List<RowDto>? Rows { get; init; }
    }

    private static async Task<IResult> SetSubjectRubricAsync(
        OdinDbContext db, Guid subjectId, [FromBody] SubjectRubricBody body, CancellationToken ct)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == subjectId && s.DeletedAt == null, ct);
        if (subject is null) return Results.NotFound();

        // Dropping a custom rubric soft-deletes the owned template; saved
        // student scores stay attached to its (soft-deleted) rows.
        async Task RetireOwnedAsync()
        {
            var owned = await db.RubricTemplates
                .Where(t => t.OwnerSubjectId == subjectId && !t.IsShared && t.DeletedAt == null)
                .ToListAsync(ct);
            foreach (var t in owned) t.DeletedAt = DateTime.UtcNow;
        }

        switch (body.Mode)
        {
            case "none":
                subject.RubricTemplateId = null;
                await RetireOwnedAsync();
                break;

            case "shared":
                var shared = await db.RubricTemplates.FirstOrDefaultAsync(
                    t => t.RubricTemplateId == body.TemplateId && t.IsShared && t.DeletedAt == null, ct);
                if (shared is null) return Results.BadRequest(new { error = "Unknown rubric template." });
                subject.RubricTemplateId = shared.RubricTemplateId;
                await RetireOwnedAsync();
                break;

            case "custom":
                var rows = body.Rows ?? [];
                if (ValidateRows(rows) is { } error) return Results.BadRequest(new { error });
                var owned = await db.RubricTemplates
                    .FirstOrDefaultAsync(t => t.OwnerSubjectId == subjectId && !t.IsShared, ct);
                if (owned is null)
                {
                    owned = new RubricTemplate { Name = $"{subject.Code} rubric", IsShared = false, OwnerSubjectId = subjectId };
                    db.RubricTemplates.Add(owned);
                }
                owned.DeletedAt = null;
                await ReconcileRowsAsync(db, owned.RubricTemplateId, rows, ct);
                subject.RubricTemplateId = owned.RubricTemplateId;
                break;

            default:
                return Results.BadRequest(new { error = "Mode must be none, shared or custom." });
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { subjectId, mode = body.Mode, templateId = subject.RubricTemplateId });
    }
}

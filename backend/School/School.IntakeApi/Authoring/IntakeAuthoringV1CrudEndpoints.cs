using System.Text.Json;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace School.IntakeApi.Authoring;

/// <summary>
/// Admin CRUD for the three simple intake authoring libraries ported from
/// QuVian core: field-library entries (reusable builder blocks), text
/// templates (rich-text merge documents) and generation rules (answers →
/// which document templates). Routes and response shapes match the core
/// intakeApi client; isFirmLibrary/groupId are echoed as constants since
/// IBSS is single-tenant.
/// </summary>
[Route("/v1/intake/field-library-entries")]
[EndpointTag("Intake.Authoring")]
public sealed class IntakeAuthoringV1CrudEndpoints : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        // Field library
        app.MapGet("/v1/intake/field-library-entries", FieldsListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/field-library-entries", FieldsCreateAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/field-library-entries/{id:guid}", FieldsGetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/intake/field-library-entries/{id:guid}", FieldsUpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/intake/field-library-entries/{id:guid}", FieldsDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/field-library-entries/{id:guid}/restore", FieldsRestoreAsync).RequireAuthorization("AdminOnly");

        // Text templates
        app.MapGet("/v1/intake/text-templates", TextListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/text-templates", TextCreateAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/text-templates/{id:guid}", TextGetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/intake/text-templates/{id:guid}", TextUpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/intake/text-templates/{id:guid}", TextDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/text-templates/{id:guid}/restore", TextRestoreAsync).RequireAuthorization("AdminOnly");

        // Generation rules
        app.MapGet("/v1/intake/generation-rules", RulesListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/generation-rules", RulesCreateAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/generation-rules/{id:guid}", RulesGetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/intake/generation-rules/{id:guid}", RulesUpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/intake/generation-rules/{id:guid}", RulesDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/generation-rules/{id:guid}/restore", RulesRestoreAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult Ok(object data) => Results.Ok(new { success = true, data });
    private static IResult Fail(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { success = false, error }, statusCode: status);
    private static string? Caller(HttpContext http) => http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private static bool IsValidJson(string s)
    {
        try { using var _ = JsonDocument.Parse(s); return true; }
        catch { return false; }
    }

    // ── Field library ─────────────────────────────────────────────────────

    public sealed class FieldWriteRequest
    {
        public string? Name { get; init; }
        public string? Category { get; init; }
        public string? DefinitionJson { get; init; }
    }

    private static object FieldDto(FieldLibraryEntry e) => new
    {
        fieldLibraryEntryId = e.FieldLibraryEntryId,
        name = e.Name,
        category = e.Category,
        definitionJson = e.DefinitionJson,
        createdByUserId = e.CreatedByUserId,
        createdAt = e.CreatedAt,
        modifiedAt = e.ModifiedAt,
        deletedAt = e.DeletedAt,
    };

    private static async Task<IResult> FieldsListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.FieldLibraryEntries
            .Where(e => includeDeleted || e.DeletedAt == null)
            .OrderBy(e => e.Category).ThenBy(e => e.Name)
            .ToListAsync(ct);
        return Ok(new { items = items.Select(FieldDto) });
    }

    private static async Task<IResult> FieldsCreateAsync(
        [FromBody] FieldWriteRequest body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (string.IsNullOrWhiteSpace(body.DefinitionJson) || !IsValidJson(body.DefinitionJson))
            return Fail("definition_required");
        var e = new FieldLibraryEntry
        {
            Name = body.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(body.Category) ? "general" : body.Category.Trim(),
            DefinitionJson = body.DefinitionJson,
            CreatedByUserId = Caller(http),
        };
        db.FieldLibraryEntries.Add(e);
        await db.SaveChangesAsync(ct);
        return Ok(FieldDto(e));
    }

    private static async Task<IResult> FieldsGetAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.FieldLibraryEntries.FirstOrDefaultAsync(x => x.FieldLibraryEntryId == id, ct);
        return e is null ? Fail("not_found", StatusCodes.Status404NotFound) : Ok(FieldDto(e));
    }

    private static async Task<IResult> FieldsUpdateAsync(
        Guid id, [FromBody] FieldWriteRequest body, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.FieldLibraryEntries.FirstOrDefaultAsync(x => x.FieldLibraryEntryId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (string.IsNullOrWhiteSpace(body.DefinitionJson) || !IsValidJson(body.DefinitionJson))
            return Fail("definition_required");
        e.Name = body.Name.Trim();
        if (!string.IsNullOrWhiteSpace(body.Category)) e.Category = body.Category.Trim();
        e.DefinitionJson = body.DefinitionJson;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(FieldDto(e));
    }

    private static async Task<IResult> FieldsDeleteAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.FieldLibraryEntries.FirstOrDefaultAsync(x => x.FieldLibraryEntryId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        e.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }

    private static async Task<IResult> FieldsRestoreAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.FieldLibraryEntries.FirstOrDefaultAsync(x => x.FieldLibraryEntryId == id && x.DeletedAt != null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        e.DeletedAt = null;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(FieldDto(e));
    }

    // ── Text templates ────────────────────────────────────────────────────

    public sealed class TextWriteRequest
    {
        public string? Name { get; init; }
        public string? BodyJson { get; init; }
    }

    private static object TextDto(TextTemplate e) => new
    {
        textTemplateId = e.TextTemplateId,
        name = e.Name,
        bodyJson = e.BodyJson,
        isFirmLibrary = true,
        groupId = (Guid?)null,
        createdByUserId = e.CreatedByUserId,
        createdAt = e.CreatedAt,
        modifiedAt = e.ModifiedAt,
        deletedAt = e.DeletedAt,
    };

    private static async Task<IResult> TextListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.IntakeTextTemplates
            .Where(e => includeDeleted || e.DeletedAt == null)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
        return Ok(new { items = items.Select(TextDto) });
    }

    private static async Task<IResult> TextCreateAsync(
        [FromBody] TextWriteRequest body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (string.IsNullOrWhiteSpace(body.BodyJson)) return Fail("body_required");
        var e = new TextTemplate
        {
            Name = body.Name.Trim(),
            BodyJson = body.BodyJson,
            CreatedByUserId = Caller(http),
        };
        db.IntakeTextTemplates.Add(e);
        await db.SaveChangesAsync(ct);
        return Ok(TextDto(e));
    }

    private static async Task<IResult> TextGetAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeTextTemplates.FirstOrDefaultAsync(x => x.TextTemplateId == id, ct);
        return e is null ? Fail("not_found", StatusCodes.Status404NotFound) : Ok(TextDto(e));
    }

    private static async Task<IResult> TextUpdateAsync(
        Guid id, [FromBody] TextWriteRequest body, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeTextTemplates.FirstOrDefaultAsync(x => x.TextTemplateId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (string.IsNullOrWhiteSpace(body.BodyJson)) return Fail("body_required");
        e.Name = body.Name.Trim();
        e.BodyJson = body.BodyJson;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(TextDto(e));
    }

    private static async Task<IResult> TextDeleteAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeTextTemplates.FirstOrDefaultAsync(x => x.TextTemplateId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        e.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }

    private static async Task<IResult> TextRestoreAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.IntakeTextTemplates.FirstOrDefaultAsync(x => x.TextTemplateId == id && x.DeletedAt != null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        e.DeletedAt = null;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(TextDto(e));
    }

    // ── Generation rules ──────────────────────────────────────────────────

    public sealed class RuleWriteRequest
    {
        public string? Name { get; init; }
        public string? RuleJson { get; init; }
        public string? IncludeDocumentTemplateIdsCsv { get; init; }
    }

    private static object RuleDto(GenerationRule e) => new
    {
        generationRuleId = e.GenerationRuleId,
        name = e.Name,
        ruleJson = e.RuleJson,
        includeDocumentTemplateIdsCsv = e.IncludeDocumentTemplateIdsCsv,
        isFirmLibrary = true,
        groupId = (Guid?)null,
        createdByUserId = e.CreatedByUserId,
        createdAt = e.CreatedAt,
        modifiedAt = e.ModifiedAt,
        deletedAt = e.DeletedAt,
    };

    private static async Task<IResult> RulesListAsync(OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.GenerationRules
            .Where(e => includeDeleted || e.DeletedAt == null)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
        return Ok(new { items = items.Select(RuleDto) });
    }

    private static async Task<IResult> RulesCreateAsync(
        [FromBody] RuleWriteRequest body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (string.IsNullOrWhiteSpace(body.RuleJson) || !IsValidJson(body.RuleJson)) return Fail("rule_required");
        var e = new GenerationRule
        {
            Name = body.Name.Trim(),
            RuleJson = body.RuleJson,
            IncludeDocumentTemplateIdsCsv = body.IncludeDocumentTemplateIdsCsv?.Trim() ?? "",
            CreatedByUserId = Caller(http),
        };
        db.GenerationRules.Add(e);
        await db.SaveChangesAsync(ct);
        return Ok(RuleDto(e));
    }

    private static async Task<IResult> RulesGetAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.GenerationRules.FirstOrDefaultAsync(x => x.GenerationRuleId == id, ct);
        return e is null ? Fail("not_found", StatusCodes.Status404NotFound) : Ok(RuleDto(e));
    }

    private static async Task<IResult> RulesUpdateAsync(
        Guid id, [FromBody] RuleWriteRequest body, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.GenerationRules.FirstOrDefaultAsync(x => x.GenerationRuleId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (string.IsNullOrWhiteSpace(body.RuleJson) || !IsValidJson(body.RuleJson)) return Fail("rule_required");
        e.Name = body.Name.Trim();
        e.RuleJson = body.RuleJson;
        e.IncludeDocumentTemplateIdsCsv = body.IncludeDocumentTemplateIdsCsv?.Trim() ?? e.IncludeDocumentTemplateIdsCsv;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(RuleDto(e));
    }

    private static async Task<IResult> RulesDeleteAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.GenerationRules.FirstOrDefaultAsync(x => x.GenerationRuleId == id && x.DeletedAt == null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        e.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }

    private static async Task<IResult> RulesRestoreAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var e = await db.GenerationRules.FirstOrDefaultAsync(x => x.GenerationRuleId == id && x.DeletedAt != null, ct);
        if (e is null) return Fail("not_found", StatusCodes.Status404NotFound);
        e.DeletedAt = null;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(RuleDto(e));
    }
}

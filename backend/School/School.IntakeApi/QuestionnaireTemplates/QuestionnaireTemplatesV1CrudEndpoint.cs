using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace School.IntakeApi.QuestionnaireTemplates;

/// <summary>
/// CRUD for questionnaire templates — the authoring plane of the intake
/// system ported from QuVian core. Routes and response envelope
/// ({ success, data }) intentionally match the core frontend's
/// intakeApi client so the copied builder UI works against IBSS with
/// minimal rewiring. Admin-only: templates are authored by the
/// Admission Office; fill-out surfaces come in later phases.
/// </summary>
[Route("/v1/intake/questionnaire-templates")]
[EndpointTag("Intake.QuestionnaireTemplates")]
public sealed class QuestionnaireTemplatesV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/intake/questionnaire-templates", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/questionnaire-templates", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/questionnaire-templates/{questionnaireTemplateId:guid}", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/intake/questionnaire-templates/{questionnaireTemplateId:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/intake/questionnaire-templates/{questionnaireTemplateId:guid}", SoftDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/questionnaire-templates/{questionnaireTemplateId:guid}/restore", RestoreAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class WriteRequest
    {
        public string? Name { get; init; }
        public string? Version { get; init; }
        public string? DefinitionJson { get; init; }
    }

    private static string Hash(string definitionJson) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(definitionJson))).ToUpperInvariant();

    private static IResult Ok(object data) => Results.Ok(new { success = true, data });
    private static IResult Fail(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { success = false, error }, statusCode: status);

    private static bool IsValidJson(string s)
    {
        try { using var _ = JsonDocument.Parse(s); return true; }
        catch { return false; }
    }

    private static object ToListItem(QuestionnaireTemplate t) => new
    {
        questionnaireTemplateId = t.QuestionnaireTemplateId,
        name = t.Name,
        version = t.Version,
        definitionHash = t.DefinitionHash,
        createdByUserId = t.CreatedByUserId,
        createdAt = t.CreatedAt,
        modifiedAt = t.ModifiedAt,
        deletedAt = t.DeletedAt,
    };

    private static async Task<IResult> ListAsync(
        OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.QuestionnaireTemplates
            .Where(t => includeDeleted || t.DeletedAt == null)
            .OrderBy(t => t.DeletedAt != null)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
        return Ok(new { items = items.Select(ToListItem) });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] WriteRequest body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (string.IsNullOrWhiteSpace(body.DefinitionJson)) return Fail("definition_required");
        if (!IsValidJson(body.DefinitionJson)) return Fail("definition_not_valid_json");

        var now = DateTime.UtcNow;
        var entity = new QuestionnaireTemplate
        {
            QuestionnaireTemplateId = Guid.NewGuid(),
            Name = body.Name.Trim(),
            Version = string.IsNullOrWhiteSpace(body.Version) ? "1.0.0" : body.Version.Trim(),
            DefinitionJson = body.DefinitionJson,
            DefinitionHash = Hash(body.DefinitionJson),
            CreatedByUserId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            CreatedAt = now,
            ModifiedAt = now,
        };
        db.QuestionnaireTemplates.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            questionnaireTemplateId = entity.QuestionnaireTemplateId,
            name = entity.Name,
            version = entity.Version,
            definitionHash = entity.DefinitionHash,
            createdAt = entity.CreatedAt,
        });
    }

    private static async Task<IResult> GetAsync(
        Guid questionnaireTemplateId, OdinDbContext db, CancellationToken ct)
    {
        var t = await db.QuestionnaireTemplates
            .FirstOrDefaultAsync(x => x.QuestionnaireTemplateId == questionnaireTemplateId, ct);
        if (t is null) return Fail("not_found", StatusCodes.Status404NotFound);
        return Ok(new
        {
            questionnaireTemplateId = t.QuestionnaireTemplateId,
            name = t.Name,
            version = t.Version,
            definitionJson = t.DefinitionJson,
            definitionHash = t.DefinitionHash,
            createdByUserId = t.CreatedByUserId,
            createdAt = t.CreatedAt,
            modifiedAt = t.ModifiedAt,
            deletedAt = t.DeletedAt,
        });
    }

    private static async Task<IResult> UpdateAsync(
        Guid questionnaireTemplateId, [FromBody] WriteRequest body, OdinDbContext db, CancellationToken ct)
    {
        var t = await db.QuestionnaireTemplates
            .FirstOrDefaultAsync(x => x.QuestionnaireTemplateId == questionnaireTemplateId && x.DeletedAt == null, ct);
        if (t is null) return Fail("not_found", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (string.IsNullOrWhiteSpace(body.DefinitionJson)) return Fail("definition_required");
        if (!IsValidJson(body.DefinitionJson)) return Fail("definition_not_valid_json");

        t.Name = body.Name.Trim();
        if (!string.IsNullOrWhiteSpace(body.Version)) t.Version = body.Version.Trim();
        t.DefinitionJson = body.DefinitionJson;
        t.DefinitionHash = Hash(body.DefinitionJson);
        t.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ToListItem(t));
    }

    private static async Task<IResult> SoftDeleteAsync(
        Guid questionnaireTemplateId, OdinDbContext db, CancellationToken ct)
    {
        var t = await db.QuestionnaireTemplates
            .FirstOrDefaultAsync(x => x.QuestionnaireTemplateId == questionnaireTemplateId && x.DeletedAt == null, ct);
        if (t is null) return Fail("not_found", StatusCodes.Status404NotFound);
        t.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }

    private static async Task<IResult> RestoreAsync(
        Guid questionnaireTemplateId, OdinDbContext db, CancellationToken ct)
    {
        var t = await db.QuestionnaireTemplates
            .FirstOrDefaultAsync(x => x.QuestionnaireTemplateId == questionnaireTemplateId && x.DeletedAt != null, ct);
        if (t is null) return Fail("not_found", StatusCodes.Status404NotFound);
        t.DeletedAt = null;
        t.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ToListItem(t));
    }
}

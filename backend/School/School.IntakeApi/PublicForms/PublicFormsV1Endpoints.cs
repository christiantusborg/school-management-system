using System.Security.Cryptography;
using System.Text.Json;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace School.IntakeApi.PublicForms;

/// <summary>
/// Public link forms. Admin side (Admin-guarded /v1/intake prefix): CRUD +
/// submissions list/detail. Public side (/v1/public prefix, anonymous): GET
/// the published form definition by slug, POST a submission with plain
/// answers. No registration, no KEM, payments disabled — all by explicit
/// decision; the price fields are carried but ignored by the fill flow.
/// </summary>
[Route("/v1/intake/public-forms")]
[EndpointTag("Intake.PublicForms")]
public sealed class PublicFormsV1Endpoints : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/intake/public-forms", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/public-forms", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/intake/public-forms/{id:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/intake/public-forms/{id:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/public-forms/{id:guid}/submissions", SubmissionsAsync).RequireAuthorization("AdminOnly");

        app.MapGet("/v1/public/forms/{slug}", PublicGetAsync).AllowAnonymous();
        app.MapPost("/v1/public/forms/{slug}/submit", PublicSubmitAsync).AllowAnonymous();
        return app;
    }

    private static IResult Ok(object data) => Results.Ok(new { success = true, data });
    private static IResult Fail(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { success = false, error }, statusCode: status);

    public sealed class WriteRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public Guid? QuestionnaireTemplateId { get; init; }
        public bool IsPublished { get; init; }
    }

    public sealed class SubmitRequest
    {
        public string? AnswersJson { get; init; }
        public string? RespondentEmail { get; init; }
        public string? RespondentName { get; init; }
    }

    private static object Dto(PublicForm f, int submissionCount) => new
    {
        publicFormId = f.PublicFormId,
        name = f.Name,
        description = f.Description,
        slug = f.Slug,
        questionnaireTemplateId = f.QuestionnaireTemplateId,
        isPublished = f.IsPublished,
        submissionCount,
        createdAt = f.CreatedAt,
        modifiedAt = f.ModifiedAt,
    };

    private static string NewSlug()
    {
        // 10-char base32-ish token: unguessable enough for an unlisted URL.
        var bytes = RandomNumberGenerator.GetBytes(8);
        return Convert.ToHexString(bytes).ToLowerInvariant()[..10];
    }

    // ── Admin ─────────────────────────────────────────────────────────────

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.PublicForms
            .Where(f => f.DeletedAt == null)
            .OrderBy(f => f.Name)
            .Select(f => new
            {
                Entity = f,
                Count = db.PublicFormSubmissions.Count(s => s.PublicFormId == f.PublicFormId && s.DeletedAt == null),
            })
            .ToListAsync(ct);
        return Ok(new { items = items.Select(x => Dto(x.Entity, x.Count)) });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] WriteRequest body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (body.QuestionnaireTemplateId is not { } tid
            || !await db.QuestionnaireTemplates.AnyAsync(t => t.QuestionnaireTemplateId == tid && t.DeletedAt == null, ct))
            return Fail("template_required");

        var f = new PublicForm
        {
            Name = body.Name.Trim(),
            Description = body.Description?.Trim(),
            Slug = NewSlug(),
            QuestionnaireTemplateId = tid,
            IsPublished = body.IsPublished,
            CreatedByUserId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        };
        db.PublicForms.Add(f);
        await db.SaveChangesAsync(ct);
        return Ok(Dto(f, 0));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, [FromBody] WriteRequest body, OdinDbContext db, CancellationToken ct)
    {
        var f = await db.PublicForms.FirstOrDefaultAsync(x => x.PublicFormId == id && x.DeletedAt == null, ct);
        if (f is null) return Fail("not_found", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (body.QuestionnaireTemplateId is not { } tid
            || !await db.QuestionnaireTemplates.AnyAsync(t => t.QuestionnaireTemplateId == tid && t.DeletedAt == null, ct))
            return Fail("template_required");

        f.Name = body.Name.Trim();
        f.Description = body.Description?.Trim();
        f.QuestionnaireTemplateId = tid;
        f.IsPublished = body.IsPublished;
        f.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var count = await db.PublicFormSubmissions.CountAsync(s => s.PublicFormId == id && s.DeletedAt == null, ct);
        return Ok(Dto(f, count));
    }

    private static async Task<IResult> DeleteAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var f = await db.PublicForms.FirstOrDefaultAsync(x => x.PublicFormId == id && x.DeletedAt == null, ct);
        if (f is null) return Fail("not_found", StatusCodes.Status404NotFound);
        f.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }

    private static async Task<IResult> SubmissionsAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var items = await db.PublicFormSubmissions
            .Where(s => s.PublicFormId == id && s.DeletedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                publicFormSubmissionId = s.PublicFormSubmissionId,
                respondentName = s.RespondentName,
                respondentEmail = s.RespondentEmail,
                answersJson = s.AnswersJson,
                questionnaireVersionHash = s.QuestionnaireVersionHash,
                createdAt = s.CreatedAt,
            })
            .ToListAsync(ct);
        return Ok(new { items });
    }

    // ── Public fill (anonymous) ───────────────────────────────────────────

    private static async Task<IResult> PublicGetAsync(string slug, OdinDbContext db, CancellationToken ct)
    {
        var f = await db.PublicForms
            .Where(x => x.Slug == slug && x.DeletedAt == null && x.IsPublished
                && x.QuestionnaireTemplate.DeletedAt == null)
            .Select(x => new
            {
                name = x.Name,
                description = x.Description,
                definitionJson = x.QuestionnaireTemplate.DefinitionJson,
            })
            .FirstOrDefaultAsync(ct);
        if (f is null) return Results.NotFound();
        return Results.Ok(f);
    }

    private static async Task<IResult> PublicSubmitAsync(
        string slug, [FromBody] SubmitRequest body, OdinDbContext db, CancellationToken ct)
    {
        var f = await db.PublicForms
            .Include(x => x.QuestionnaireTemplate)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.DeletedAt == null && x.IsPublished, ct);
        if (f is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(body.AnswersJson))
            return Results.BadRequest(new { error = "answers_required" });
        try { using var _ = JsonDocument.Parse(body.AnswersJson); }
        catch { return Results.BadRequest(new { error = "answers_not_valid_json" }); }
        // Basic flood guard: cap payload size (the questionnaire is bounded anyway).
        if (body.AnswersJson.Length > 256 * 1024)
            return Results.BadRequest(new { error = "answers_too_large" });

        var s = new PublicFormSubmission
        {
            PublicFormId = f.PublicFormId,
            AnswersJson = body.AnswersJson,
            QuestionnaireVersionHash = f.QuestionnaireTemplate.DefinitionHash,
            RespondentEmail = string.IsNullOrWhiteSpace(body.RespondentEmail) ? null : body.RespondentEmail.Trim(),
            RespondentName = string.IsNullOrWhiteSpace(body.RespondentName) ? null : body.RespondentName.Trim(),
        };
        db.PublicFormSubmissions.Add(s);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { submitted = true });
    }
}

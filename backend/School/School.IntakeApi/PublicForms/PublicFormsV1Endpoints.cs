using System.Security.Cryptography;
using System.Text.Json;
using School.IntakeApi.Stats;
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
        // Real-question statistics for one run, or combined across runs (?ids=a,b,c).
        app.MapGet("/v1/intake/public-forms/{id:guid}/stats", StatsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/public-forms/stats", CombinedStatsAsync).RequireAuthorization("AdminOnly");

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
        public DateTime? RunStartDate { get; init; }
        public DateTime? RunEndDate { get; init; }
        /// <summary>Per-form values for the questionnaire's reference fields.</summary>
        public List<OwnerReferenceValueInput>? OwnerReferences { get; init; }
    }

    public sealed class OwnerReferenceValueInput
    {
        public string? FieldId { get; init; }
        public string? Type { get; init; }
        public string? Value { get; init; }        // schoolId / partnerId / programmeId / free text
        public string? PartnerValue { get; init; }  // refPartnerProgramme: the chosen partnerId
        public string? Display { get; init; }        // human label ("IBSS", "Acme — MBA", the text)
    }

    public sealed class SubmitRequest
    {
        public string? AnswersJson { get; init; }
        public string? RespondentEmail { get; init; }
        public string? RespondentName { get; init; }
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private sealed record OwnerRefValue(string? FieldId, string? Type, string? Value, string? PartnerValue, string? Display);

    private static List<OwnerRefValue> ParseOwnerRefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<List<OwnerRefValue>>(json, JsonOpts) ?? new(); }
        catch { return new(); }
    }

    // Serialize the owner input for storage; null when there is nothing to keep.
    private static string? SerializeOwnerRefs(List<OwnerReferenceValueInput>? input)
    {
        var vals = (input ?? new())
            .Where(v => !string.IsNullOrWhiteSpace(v.FieldId))
            .Select(v => new { fieldId = v.FieldId, type = v.Type, value = v.Value, partnerValue = v.PartnerValue, display = v.Display })
            .ToList();
        return vals.Count == 0 ? null : JsonSerializer.Serialize(vals, JsonOpts);
    }

    private static object? OwnerRefsDto(string? json)
    {
        var r = ParseOwnerRefs(json);
        if (r.Count == 0) return null;
        return r.Select(v => new { fieldId = v.FieldId, type = v.Type, value = v.Value, partnerValue = v.PartnerValue, display = v.Display });
    }

    // Every reference field in the questionnaire must have a value before the
    // run can be published. Returns an error code, or null when satisfied.
    private static string? ValidateReferencesForPublish(string? definitionJson, List<OwnerReferenceValueInput>? input)
    {
        var fields = QuestionnaireStats.ExtractReferenceFields(definitionJson);
        if (fields.Count == 0) return null;
        var byId = (input ?? new())
            .Where(v => !string.IsNullOrWhiteSpace(v.FieldId))
            .GroupBy(v => v.FieldId!).ToDictionary(g => g.Key, g => g.Last());
        foreach (var f in fields)
            if (!byId.TryGetValue(f.Id, out var v) || string.IsNullOrWhiteSpace(v.Value))
                return "references_required";
        return null;
    }

    // Read-only context shown on the fill page: each reference field's label
    // paired with the owner's chosen display value.
    private static List<object> BuildReferenceContext(string? definitionJson, string? ownerReferencesJson)
    {
        var fields = QuestionnaireStats.ExtractReferenceFields(definitionJson);
        if (fields.Count == 0) return new();
        var refs = ParseOwnerRefs(ownerReferencesJson);
        return fields
            .Select(f => new { field = f, val = refs.FirstOrDefault(x => x.FieldId == f.Id) })
            .Where(x => !string.IsNullOrWhiteSpace(x.val?.Display))
            .Select(x => (object)new { fieldId = x.field.Id, label = x.field.Label, display = x.val!.Display })
            .ToList();
    }

    // Stamp the owner reference values into a submission's answers (keyed by
    // field id) so they aggregate generically in statistics.
    private static string StampReferenceAnswers(string answersJson, List<OwnerRefValue> refs)
    {
        if (refs.Count == 0) return answersJson;
        try
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(answersJson) as System.Text.Json.Nodes.JsonObject
                ?? new System.Text.Json.Nodes.JsonObject();
            var target = root["answers"] as System.Text.Json.Nodes.JsonObject ?? root;
            foreach (var r in refs)
                if (!string.IsNullOrWhiteSpace(r.FieldId) && !string.IsNullOrWhiteSpace(r.Display))
                    target[r.FieldId!] = r.Display;
            return root.ToJsonString();
        }
        catch { return answersJson; }
    }

    private static object Dto(PublicForm f, int submissionCount, string? questionnaireName = null) => new
    {
        publicFormId = f.PublicFormId,
        name = f.Name,
        description = f.Description,
        slug = f.Slug,
        questionnaireTemplateId = f.QuestionnaireTemplateId,
        questionnaireName,
        isPublished = f.IsPublished,
        runStartDate = f.RunStartDate,
        runEndDate = f.RunEndDate,
        // Closed once past the end date (end-date enforcement; start is a label).
        isClosed = f.RunEndDate is { } end && DateTime.UtcNow > end,
        ownerReferences = OwnerRefsDto(f.OwnerReferencesJson),
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
                QuestionnaireName = f.QuestionnaireTemplate.Name,
                Count = db.PublicFormSubmissions.Count(s => s.PublicFormId == f.PublicFormId && s.DeletedAt == null),
            })
            .ToListAsync(ct);
        return Ok(new { items = items.Select(x => Dto(x.Entity, x.Count, x.QuestionnaireName)) });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] WriteRequest body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (body.QuestionnaireTemplateId is not { } tid) return Fail("template_required");
        var template = await db.QuestionnaireTemplates
            .Where(t => t.QuestionnaireTemplateId == tid && t.DeletedAt == null)
            .Select(t => new { t.DefinitionJson }).FirstOrDefaultAsync(ct);
        if (template is null) return Fail("template_required");
        if (body.IsPublished && ValidateReferencesForPublish(template.DefinitionJson, body.OwnerReferences) is { } refErr)
            return Fail(refErr);

        var f = new PublicForm
        {
            Name = body.Name.Trim(),
            Description = body.Description?.Trim(),
            Slug = NewSlug(),
            QuestionnaireTemplateId = tid,
            IsPublished = body.IsPublished,
            RunStartDate = body.RunStartDate,
            RunEndDate = body.RunEndDate,
            OwnerReferencesJson = SerializeOwnerRefs(body.OwnerReferences),
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
        if (body.QuestionnaireTemplateId is not { } tid) return Fail("template_required");
        var template = await db.QuestionnaireTemplates
            .Where(t => t.QuestionnaireTemplateId == tid && t.DeletedAt == null)
            .Select(t => new { t.DefinitionJson }).FirstOrDefaultAsync(ct);
        if (template is null) return Fail("template_required");
        if (body.IsPublished && ValidateReferencesForPublish(template.DefinitionJson, body.OwnerReferences) is { } refErr)
            return Fail(refErr);

        f.Name = body.Name.Trim();
        f.Description = body.Description?.Trim();
        f.QuestionnaireTemplateId = tid;
        f.IsPublished = body.IsPublished;
        f.RunStartDate = body.RunStartDate;
        f.RunEndDate = body.RunEndDate;
        f.OwnerReferencesJson = SerializeOwnerRefs(body.OwnerReferences);
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
                runEndDate = x.RunEndDate,
                definitionJson = x.QuestionnaireTemplate.DefinitionJson,
                ownerReferencesJson = x.OwnerReferencesJson,
            })
            .FirstOrDefaultAsync(ct);
        if (f is null) return Results.NotFound();
        // End-date enforcement: past the run's end date the form is closed —
        // return the name + closed flag (no definition) so the fill page can
        // show a friendly "this form is closed" message.
        if (f.runEndDate is { } end && DateTime.UtcNow > end)
            return Results.Ok(new { name = f.name, closed = true, runEndDate = f.runEndDate });
        // references = owner-set values shown read-only above the form and
        // seeded into the submitted answers.
        var references = BuildReferenceContext(f.definitionJson, f.ownerReferencesJson);
        return Results.Ok(new { f.name, f.description, f.definitionJson, references, closed = false });
    }

    private static async Task<IResult> PublicSubmitAsync(
        string slug, [FromBody] SubmitRequest body, OdinDbContext db, CancellationToken ct)
    {
        var f = await db.PublicForms
            .Include(x => x.QuestionnaireTemplate)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.DeletedAt == null && x.IsPublished, ct);
        if (f is null) return Results.NotFound();
        if (f.RunEndDate is { } end && DateTime.UtcNow > end)
            return Results.BadRequest(new { error = "form_closed" });

        if (string.IsNullOrWhiteSpace(body.AnswersJson))
            return Results.BadRequest(new { error = "answers_required" });
        try { using var _ = JsonDocument.Parse(body.AnswersJson); }
        catch { return Results.BadRequest(new { error = "answers_not_valid_json" }); }
        // Basic flood guard: cap payload size (the questionnaire is bounded anyway).
        if (body.AnswersJson.Length > 256 * 1024)
            return Results.BadRequest(new { error = "answers_too_large" });

        // Stamp the owner's per-form reference values into the answers so they
        // aggregate generically in statistics (authoritative, server-side).
        var answers = StampReferenceAnswers(body.AnswersJson, ParseOwnerRefs(f.OwnerReferencesJson));

        var s = new PublicFormSubmission
        {
            PublicFormId = f.PublicFormId,
            AnswersJson = answers,
            QuestionnaireVersionHash = f.QuestionnaireTemplate.DefinitionHash,
            RespondentEmail = string.IsNullOrWhiteSpace(body.RespondentEmail) ? null : body.RespondentEmail.Trim(),
            RespondentName = string.IsNullOrWhiteSpace(body.RespondentName) ? null : body.RespondentName.Trim(),
        };
        db.PublicFormSubmissions.Add(s);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { submitted = true });
    }

    // ── Statistics (real questions, aggregates, comments) ─────────────────

    private static async Task<IResult> StatsAsync(Guid id, OdinDbContext db, CancellationToken ct)
    {
        var form = await db.PublicForms
            .Include(f => f.QuestionnaireTemplate)
            .FirstOrDefaultAsync(f => f.PublicFormId == id && f.DeletedAt == null, ct);
        if (form?.QuestionnaireTemplate is null) return Fail("not_found", StatusCodes.Status404NotFound);
        return Ok(await BuildStatsAsync(db, form.QuestionnaireTemplateId, [id], form.Name,
            form.QuestionnaireTemplate.Name, form.QuestionnaireTemplate.Version, ct));
    }

    /// <summary>Combined statistics across several runs (?ids=a,b,c) of the
    /// same questionnaire. Sets versionWarning when the pooled submissions span
    /// more than one questionnaire version or a question changed/was removed.</summary>
    private static async Task<IResult> CombinedStatsAsync(
        [FromQuery] string? ids, OdinDbContext db, CancellationToken ct)
    {
        var idList = (ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty).Distinct().ToArray();
        if (idList.Length == 0) return Fail("ids_required");

        var forms = await db.PublicForms
            .Include(f => f.QuestionnaireTemplate)
            .Where(f => idList.Contains(f.PublicFormId) && f.DeletedAt == null)
            .OrderBy(f => f.RunStartDate).ThenBy(f => f.CreatedAt)
            .ToListAsync(ct);
        if (forms.Count == 0) return Fail("not_found", StatusCodes.Status404NotFound);

        // All selected runs must belong to one questionnaire (the UI only
        // offers runs of the same questionnaire); use the first for versions.
        var templateId = forms[0].QuestionnaireTemplateId;
        var qName = forms[0].QuestionnaireTemplate?.Name ?? "";
        var version = forms[0].QuestionnaireTemplate?.Version ?? "";
        var name = string.Join(" + ", forms.Select(f => f.Name));
        return Ok(await BuildStatsAsync(db, templateId, forms.Select(f => f.PublicFormId).ToArray(),
            name, qName, version, ct));
    }

    private static async Task<List<QuestionnaireStats.VersionInfo>> LoadVersionsAsync(
        OdinDbContext db, Guid templateId, CancellationToken ct)
    {
        var versions = new List<QuestionnaireStats.VersionInfo>();
        var frozen = await db.QuestionnaireTemplateVersions
            .Where(v => v.QuestionnaireTemplateId == templateId)
            .OrderBy(v => v.FrozenAt).ToListAsync(ct);
        foreach (var v in frozen)
            versions.Add(new QuestionnaireStats.VersionInfo(v.Version, v.DefinitionHash, QuestionnaireStats.ParseQuestions(v.DefinitionJson)));
        var t = await db.QuestionnaireTemplates.FirstOrDefaultAsync(x => x.QuestionnaireTemplateId == templateId, ct);
        if (t is not null)
            versions.Add(new QuestionnaireStats.VersionInfo(t.Version, t.DefinitionHash, QuestionnaireStats.ParseQuestions(t.DefinitionJson)));
        return versions;
    }

    private static async Task<object> BuildStatsAsync(
        OdinDbContext db, Guid templateId, Guid[] formIds,
        string name, string questionnaireName, string currentVersion, CancellationToken ct)
    {
        var versions = await LoadVersionsAsync(db, templateId, ct);
        var subs = await db.PublicFormSubmissions
            .Where(s => formIds.Contains(s.PublicFormId) && s.DeletedAt == null)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new { s.CreatedAt, s.QuestionnaireVersionHash, s.AnswersJson })
            .ToListAsync(ct);
        var respondents = subs
            .Select((s, idx) => new QuestionnaireStats.Respondent(
                idx + 1, s.CreatedAt, s.QuestionnaireVersionHash, QuestionnaireStats.ParseAnswers(s.AnswersJson)))
            .ToList();

        var stats = QuestionnaireStats.Build(versions, respondents);
        var distinctHashes = subs.Select(s => s.QuestionnaireVersionHash)
            .Where(h => !string.IsNullOrEmpty(h)).Distinct().Count();
        var versionWarning = distinctHashes > 1 || stats.ChangedIds.Count > 0 || stats.RemovedIds.Count > 0;

        return new
        {
            name,
            templateName = questionnaireName,
            currentVersion,
            runCount = formIds.Length,
            respondentCount = respondents.Count,
            versionCount = versions.Count,
            distinctVersionCount = distinctHashes,
            versionWarning,
            questions = stats.Questions,
            respondents = QuestionnaireStats.ProjectRespondents(respondents, stats.Ordered, versions),
        };
    }
}

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace School.IntakeApi.Stats;

/// <summary>
/// Anonymous aggregate statistics for a questionnaire instance, with
/// drill-down, the text-comments section, a completion checklist and an
/// LLM chat over the answers (local Ollama, same instance as the document
/// scanner). Answers are NEVER linked to a name in any payload here — the
/// drill-down uses "Respondent #N" only. The link exists in the database
/// (IntakeResponse.StudentId) for backend use, by design.
/// </summary>
[Route("/v1/intake/intake-instances/{intakeInstanceId:guid}/stats")]
[EndpointTag("Intake.Stats")]
public sealed class IntakeStatsV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/intake/intake-instances/{intakeInstanceId:guid}/stats", StatsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/intake-instances/{intakeInstanceId:guid}/completion", CompletionAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/intake-instances/{intakeInstanceId:guid}/chat", ChatAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult Ok(object data) => Results.Ok(new { success = true, data });
    private static IResult Fail(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { success = false, error }, statusCode: status);

    // ── Definition parsing ────────────────────────────────────────────────

    private sealed record Question(string Id, string Type, string Label, List<(string Value, string Label)> Options);

    private static List<Question> ParseQuestions(string definitionJson)
    {
        var list = new List<Question>();
        try
        {
            using var doc = JsonDocument.Parse(definitionJson);
            if (!doc.RootElement.TryGetProperty("pages", out var pages)) return list;
            foreach (var page in pages.EnumerateArray())
            foreach (var section in page.TryGetProperty("sections", out var ss) ? ss.EnumerateArray() : default)
            foreach (var group in section.TryGetProperty("groups", out var gs) ? gs.EnumerateArray() : default)
            foreach (var item in group.TryGetProperty("items", out var its) ? its.EnumerateArray() : default)
            {
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                var type = item.TryGetProperty("type", out var tEl) ? tEl.GetString() : null;
                if (id is null || type is null) continue;
                if (type is "heading" or "paragraph" or "divider") continue;
                var label = item.TryGetProperty("label", out var lEl) && lEl.TryGetProperty("fallback", out var fEl)
                    ? fEl.GetString() ?? id : id;
                var options = new List<(string, string)>();
                if (item.TryGetProperty("props", out var props)
                    && props.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var o in opts.EnumerateArray())
                    {
                        var v = o.TryGetProperty("value", out var vEl) ? vEl.GetString() ?? "" : "";
                        var ol = o.TryGetProperty("label", out var olEl) ? olEl.GetString() ?? v : v;
                        options.Add((v, ol));
                    }
                }
                list.Add(new Question(id, type, label, options));
            }
        }
        catch { /* malformed definition → empty question list */ }
        return list;
    }

    private sealed record VersionInfo(string Version, string Hash, List<Question> Questions);

    private static async Task<(IntakeInstance Instance, List<VersionInfo> Versions)?> LoadAsync(
        OdinDbContext db, Guid intakeInstanceId, CancellationToken ct)
    {
        var instance = await db.IntakeInstances
            .Include(i => i.QuestionnaireTemplate)
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId && i.DeletedAt == null, ct);
        if (instance?.QuestionnaireTemplate is null) return null;

        var versions = new List<VersionInfo>();
        var frozen = await db.QuestionnaireTemplateVersions
            .Where(v => v.QuestionnaireTemplateId == instance.QuestionnaireTemplateId)
            .OrderBy(v => v.FrozenAt)
            .ToListAsync(ct);
        foreach (var v in frozen)
            versions.Add(new VersionInfo(v.Version, v.DefinitionHash, ParseQuestions(v.DefinitionJson)));
        var t = instance.QuestionnaireTemplate;
        versions.Add(new VersionInfo(t.Version, t.DefinitionHash, ParseQuestions(t.DefinitionJson)));
        return (instance, versions);
    }

    private static Dictionary<string, JsonElement> ParseAnswers(string answersJson)
    {
        try
        {
            var doc = JsonDocument.Parse(answersJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return [];
            // Renderer may nest answers under "answers".
            var root = doc.RootElement.TryGetProperty("answers", out var nested)
                && nested.ValueKind == JsonValueKind.Object ? nested : doc.RootElement;
            return root.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch { return []; }
    }

    private static string? AnswerToString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.ToString(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Array => string.Join(", ", el.EnumerateArray().Select(x => x.ToString())),
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => el.ToString(),
    };

    private sealed record RespondentRow(int N, DateTime? SubmittedAt, string? VersionHash, Dictionary<string, JsonElement> Answers);

    private static async Task<List<RespondentRow>> LoadRespondentsAsync(
        OdinDbContext db, Guid intakeInstanceId, CancellationToken ct)
    {
        var responses = await db.IntakeResponses
            .Where(r => r.IntakeInstanceId == intakeInstanceId && r.DeletedAt == null
                && r.LifecycleState == IntakeResponseLifecycleState.Submitted)
            .OrderBy(r => r.SubmittedAt)
            .Select(r => new { r.SubmittedAt, r.QuestionnaireVersionHash, r.AnswersJson })
            .ToListAsync(ct);
        return responses
            .Select((r, idx) => new RespondentRow(idx + 1, r.SubmittedAt, r.QuestionnaireVersionHash, ParseAnswers(r.AnswersJson)))
            .ToList();
    }

    // ── Stats ─────────────────────────────────────────────────────────────

    private static async Task<IResult> StatsAsync(
        Guid intakeInstanceId, OdinDbContext db, CancellationToken ct)
    {
        var loaded = await LoadAsync(db, intakeInstanceId, ct);
        if (loaded is null) return Fail("not_found", StatusCodes.Status404NotFound);
        var (instance, versions) = loaded.Value;

        var respondents = await LoadRespondentsAsync(db, intakeInstanceId, ct);
        var current = versions[^1];

        // Question order: current version first, then questions that only
        // exist in older (frozen) versions, flagged as removed. A question
        // whose label differs between versions is flagged as changed.
        var ordered = new List<Question>(current.Questions);
        var known = current.Questions.Select(q => q.Id).ToHashSet();
        var changed = new HashSet<string>();
        foreach (var v in versions.Take(versions.Count - 1))
        foreach (var q in v.Questions)
        {
            if (!known.Add(q.Id)) // seen — check label drift vs current
            {
                var cur = ordered.FirstOrDefault(x => x.Id == q.Id);
                if (cur is not null && cur.Label != q.Label) changed.Add(q.Id);
            }
            else ordered.Add(q);
        }
        var removedIds = ordered.Select(q => q.Id)
            .Where(id => current.Questions.All(q => q.Id != id)).ToHashSet();

        var questionStats = ordered.Select(q =>
        {
            var answered = respondents
                .Select(r => r.Answers.TryGetValue(q.Id, out var v) ? AnswerToString(v) : null)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            var isChoice = q.Options.Count > 0;
            var isText = q.Type is "text" or "textarea";

            object? choices = null; double? average = null;
            if (isChoice)
            {
                var counts = q.Options.Select(o => new
                {
                    value = o.Value,
                    label = o.Label,
                    count = answered.Count(a => a == o.Value || a == o.Label),
                }).ToList();
                var other = answered.Count - counts.Sum(c => c.count);
                choices = new { options = counts, other };
                // Numeric average when every option value parses as a number
                // (Likert 1-5, recommend 0-10).
                if (q.Options.All(o => double.TryParse(o.Value, out _)))
                {
                    var nums = answered.Select(a =>
                    {
                        if (double.TryParse(a, out var d)) return (double?)d;
                        var m = q.Options.FirstOrDefault(o => o.Label == a);
                        return double.TryParse(m.Value, out var d2) ? d2 : null;
                    }).Where(x => x != null).Select(x => x!.Value).ToList();
                    if (nums.Count > 0) average = Math.Round(nums.Average(), 2);
                }
            }

            var texts = isText
                ? respondents
                    .Select(r => new { r.N, text = r.Answers.TryGetValue(q.Id, out var v) ? AnswerToString(v) : null })
                    .Where(x => !string.IsNullOrWhiteSpace(x.text))
                    .Select(x => new { respondent = x.N, text = x.text! })
                    .ToList<object>()
                : null;

            return new
            {
                questionId = q.Id,
                label = q.Label,
                type = q.Type,
                answeredCount = answered.Count,
                isChoice,
                isText,
                choices,
                average,
                texts,
                changedBetweenVersions = changed.Contains(q.Id),
                removedInCurrentVersion = removedIds.Contains(q.Id),
            };
        }).ToList();

        return Ok(new
        {
            instanceName = instance.Name,
            templateName = instance.QuestionnaireTemplate!.Name,
            currentVersion = instance.QuestionnaireTemplate.Version,
            versionCount = versions.Count,
            respondentCount = respondents.Count,
            questions = questionStats,
            respondents = respondents.Select(r => new
            {
                respondent = r.N,
                submittedAt = r.SubmittedAt,
                version = versions.FirstOrDefault(v => v.Hash == r.VersionHash)?.Version ?? "?",
                answers = ordered.ToDictionary(q => q.Id, q =>
                    r.Answers.TryGetValue(q.Id, out var v) ? AnswerToString(v) : null),
            }),
        });
    }

    // ── Completion checklist (names only — never linked to answers) ───────

    private static async Task<IResult> CompletionAsync(
        Guid intakeInstanceId, OdinDbContext db, CancellationToken ct)
    {
        var instance = await db.IntakeInstances
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId && i.DeletedAt == null, ct);
        if (instance is null) return Fail("not_found", StatusCodes.Status404NotFound);

        async Task<string> StudentNameAsync(Guid sid)
        {
            var st = await db.Students.Where(s => s.StudentId == sid)
                .Select(s => new { s.StudentNumber, s.UserId }).FirstOrDefaultAsync(ct);
            if (st is null) return "(deleted)";
            var p = await db.UserProfiles.Where(x => x.UserId == st.UserId)
                .Select(x => new { x.FirstName, x.LastName }).FirstOrDefaultAsync(ct);
            var nm = $"{p?.FirstName} {p?.LastName}".Trim();
            return nm.Length > 0 ? $"{nm} ({st.StudentNumber})" : st.StudentNumber ?? "(unknown)";
        }

        var responderStudentIds = await db.IntakeResponses
            .Where(r => r.IntakeInstanceId == intakeInstanceId && r.DeletedAt == null
                && r.LifecycleState == IntakeResponseLifecycleState.Submitted && r.StudentId != null)
            .Select(r => r.StudentId!.Value).Distinct().ToListAsync(ct);
        var responderPartnerIds = await db.IntakeResponses
            .Where(r => r.IntakeInstanceId == intakeInstanceId && r.DeletedAt == null
                && r.LifecycleState == IntakeResponseLifecycleState.Submitted && r.PartnerId != null)
            .Select(r => r.PartnerId!.Value).Distinct().ToListAsync(ct);

        // The expected audience: for Targeted instances resolve targets to
        // students/partners; for Audience mode, everyone in the audience.
        List<Guid> expectedStudents = [];
        List<Guid> expectedPartners = [];
        if (instance.Audience == IntakeInstance.AudiencePartner)
        {
            expectedPartners = instance.AssignmentMode == IntakeInstance.ModeTargeted
                ? await db.IntakeAssignments.Where(a => a.IntakeInstanceId == intakeInstanceId && a.PartnerId != null)
                    .Select(a => a.PartnerId!.Value).Distinct().ToListAsync(ct)
                : await db.Partners.Where(p => p.DeletedAt == null).Select(p => p.PartnerId).ToListAsync(ct);
        }
        else
        {
            if (instance.AssignmentMode == IntakeInstance.ModeTargeted)
            {
                var rows = await db.IntakeAssignments
                    .Where(a => a.IntakeInstanceId == intakeInstanceId).ToListAsync(ct);
                var set = new HashSet<Guid>();
                foreach (var a in rows)
                {
                    if (a.StudentId is { } sid) set.Add(sid);
                    else if (a.PartnerId is { } pid)
                        set.UnionWith(await db.Students.Where(s => s.PartnerId == pid && s.DeletedAt == null)
                            .Select(s => s.StudentId).ToListAsync(ct));
                    else if (a.ProgrammeId is { } prid)
                        set.UnionWith(await db.Enrollments.Where(e => e.DeletedAt == null && e.Specialization.ProgrammeId == prid)
                            .Select(e => e.StudentId).ToListAsync(ct));
                    else if (a.SpecializationId is { } spid)
                        set.UnionWith(await db.Enrollments.Where(e => e.DeletedAt == null && e.SpecializationId == spid)
                            .Select(e => e.StudentId).ToListAsync(ct));
                    else if (a.SubjectId is { } suid)
                    {
                        var specOfSubject = await db.Subjects.Where(s => s.SubjectId == suid)
                            .Select(s => s.SpecializationId).FirstOrDefaultAsync(ct);
                        set.UnionWith(await db.Enrollments.Where(e => e.DeletedAt == null && e.SpecializationId == specOfSubject)
                            .Select(e => e.StudentId).ToListAsync(ct));
                    }
                }
                expectedStudents = set.ToList();
            }
            else
            {
                expectedStudents = await db.Students.Where(s => s.DeletedAt == null)
                    .Select(s => s.StudentId).ToListAsync(ct);
            }
        }

        var answered = new List<string>();
        var notAnswered = new List<string>();
        foreach (var sid in expectedStudents.Union(responderStudentIds).Distinct())
        {
            var name = await StudentNameAsync(sid);
            if (responderStudentIds.Contains(sid)) answered.Add(name); else notAnswered.Add(name);
        }
        foreach (var pid in expectedPartners.Union(responderPartnerIds).Distinct())
        {
            var name = await db.Partners.Where(p => p.PartnerId == pid).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "(deleted)";
            if (responderPartnerIds.Contains(pid)) answered.Add(name); else notAnswered.Add(name);
        }
        answered.Sort(StringComparer.OrdinalIgnoreCase);
        notAnswered.Sort(StringComparer.OrdinalIgnoreCase);

        return Ok(new { answered, notAnswered });
    }

    // ── LLM chat over the answers ─────────────────────────────────────────

    public sealed class ChatBody
    {
        public string? Question { get; init; }
        public List<ChatTurn>? History { get; init; }
    }
    public sealed class ChatTurn
    {
        public string? Role { get; init; }   // "user" | "assistant"
        public string? Content { get; init; }
    }

    private static async Task<IResult> ChatAsync(
        Guid intakeInstanceId, [FromBody] ChatBody body,
        OdinDbContext db, IHttpClientFactory httpFactory, IConfiguration config, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Question)) return Fail("question_required");
        var ollamaUrl = config["DocumentScan:OllamaUrl"];
        if (string.IsNullOrWhiteSpace(ollamaUrl))
            return Fail("llm_not_configured", StatusCodes.Status503ServiceUnavailable);
        var model = config["DocumentScan:OllamaModel"] ?? "llama3.2:latest";

        var loaded = await LoadAsync(db, intakeInstanceId, ct);
        if (loaded is null) return Fail("not_found", StatusCodes.Status404NotFound);
        var (instance, versions) = loaded.Value;
        var respondents = await LoadRespondentsAsync(db, intakeInstanceId, ct);
        var questions = versions[^1].Questions
            .Concat(versions.Take(versions.Count - 1).SelectMany(v => v.Questions))
            .GroupBy(q => q.Id).Select(g => g.First()).ToList();

        // Compact anonymous context: per-question label + every answer with
        // its respondent number. Choice answers resolved to option labels.
        var sb = new StringBuilder();
        sb.AppendLine($"QUESTIONNAIRE: {instance.QuestionnaireTemplate!.Name} — {respondents.Count} submitted responses.");
        foreach (var q in questions)
        {
            sb.AppendLine($"\nQUESTION [{q.Id}] ({q.Type}): {q.Label}");
            foreach (var r in respondents)
            {
                if (!r.Answers.TryGetValue(q.Id, out var v)) continue;
                var raw = AnswerToString(v);
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var label = q.Options.FirstOrDefault(o => o.Value == raw).Label ?? raw;
                sb.AppendLine($"  Respondent #{r.N}: {label}");
            }
        }

        var historyText = new StringBuilder();
        foreach (var t in body.History ?? [])
        {
            if (string.IsNullOrWhiteSpace(t.Content)) continue;
            historyText.AppendLine($"{(t.Role == "assistant" ? "ASSISTANT" : "ADMIN")}: {t.Content}");
        }

        var prompt =
            "You are an analyst for a business school's Admission Office. Below are the anonymous "
            + "answers to a student questionnaire. Respondents are only ever identified as numbers — never "
            + "guess or invent identities. Answer the admin's question concisely and concretely, cite counts "
            + "and respondent numbers where useful, and say so plainly when the data cannot answer the question.\n\n"
            + sb + "\n\nCONVERSATION SO FAR:\n" + historyText
            + $"\nADMIN: {body.Question!.Trim()}\nASSISTANT:";

        var http = httpFactory.CreateClient("docscan-ollama");
        var payload = JsonSerializer.Serialize(new { model, prompt, stream = false, options = new { temperature = 0.2 } });
        using var resp = await http.PostAsync(
            ollamaUrl.TrimEnd('/') + "/api/generate",
            new StringContent(payload, Encoding.UTF8, "application/json"), ct);
        if (!resp.IsSuccessStatusCode)
            return Fail("llm_unreachable", StatusCodes.Status502BadGateway);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var answer = doc.RootElement.TryGetProperty("response", out var respEl) ? respEl.GetString() ?? "" : "";
        return Ok(new { answer = answer.Trim() });
    }
}

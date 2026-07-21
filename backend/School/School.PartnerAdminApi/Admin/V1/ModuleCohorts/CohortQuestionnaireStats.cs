using System.Globalization;
using System.Text.Json;

namespace School.PartnerAdminApi.Admin.V1.ModuleCohorts;

/// <summary>
/// Builds the anonymous aggregate of a cohort's questionnaire responses.
/// Shared by the admin stats endpoint (no threshold) and the partner/teacher
/// one (locked below <c>minResponses</c> so single respondents can't be
/// identified). Answers are stored with no student link, so everything here
/// is aggregate-only by construction.
/// </summary>
internal static class CohortQuestionnaireStats
{
    private static readonly HashSet<string> DisplayTypes = new() { "heading", "paragraph", "divider" };
    private static readonly HashSet<string> TextTypes = new() { "text", "textarea", "richtext", "email", "phone", "tel" };

    public static async Task<object> BuildAsync(
        OdinDbContext db, Guid cohortId, int? minResponses, CancellationToken ct)
    {
        var attached = await (
            from q in db.ModuleCohortQuestionnaires
            join t in db.QuestionnaireTemplates on q.QuestionnaireTemplateId equals t.QuestionnaireTemplateId
            where q.ModuleCohortId == cohortId && q.DeletedAt == null && t.DeletedAt == null
            orderby q.SortOrder, q.CreatedAt
            select new { q.ModuleCohortQuestionnaireId, q.QuestionnaireTemplateId, t.Name, t.DefinitionJson })
            .ToListAsync(ct);
        var ids = attached.Select(a => a.ModuleCohortQuestionnaireId).ToList();

        var assigned = await db.ModuleCohortStudents
            .CountAsync(s => s.ModuleCohortId == cohortId && s.DeletedAt == null, ct);
        var responsesById = (await db.CohortQuestionnaireResponses
                .Where(r => ids.Contains(r.ModuleCohortQuestionnaireId))
                .Select(r => new { r.ModuleCohortQuestionnaireId, r.AnswersJson })
                .ToListAsync(ct))
            .GroupBy(r => r.ModuleCohortQuestionnaireId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.AnswersJson).ToList());
        var completedById = (await db.CohortQuestionnaireCompletions
                .Where(c => ids.Contains(c.ModuleCohortQuestionnaireId))
                .GroupBy(c => c.ModuleCohortQuestionnaireId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.Key, x => x.Count);

        var questionnaires = new List<object>();
        foreach (var a in attached)
        {
            var answers = responsesById.GetValueOrDefault(a.ModuleCohortQuestionnaireId, []);
            var completed = completedById.GetValueOrDefault(a.ModuleCohortQuestionnaireId);
            if (minResponses is { } min && answers.Count < min)
            {
                questionnaires.Add(new
                {
                    moduleCohortQuestionnaireId = a.ModuleCohortQuestionnaireId,
                    name = a.Name,
                    responses = answers.Count,
                    completed,
                    assigned,
                    locked = true,
                    requiredResponses = min,
                });
                continue;
            }
            questionnaires.Add(new
            {
                moduleCohortQuestionnaireId = a.ModuleCohortQuestionnaireId,
                name = a.Name,
                responses = answers.Count,
                completed,
                assigned,
                locked = false,
                questions = Aggregate(a.DefinitionJson, answers),
            });
        }

        return new { assigned, questionnaires };
    }

    private static List<object> Aggregate(string definitionJson, List<string> answersJsonList)
    {
        var parsedAnswers = new List<JsonElement>();
        foreach (var raw in answersJsonList)
        {
            try
            {
                var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    parsedAnswers.Add(doc.RootElement.Clone());
            }
            catch (JsonException) { /* skip malformed */ }
        }

        var questions = new List<object>();
        JsonDocument def;
        try { def = JsonDocument.Parse(definitionJson); }
        catch (JsonException) { return questions; }

        foreach (var item in WalkItems(def.RootElement))
        {
            var type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
            if (DisplayTypes.Contains(type)) continue;
            var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
            if (id.Length == 0) continue;
            var label = item.TryGetProperty("label", out var l)
                && l.ValueKind == JsonValueKind.Object
                && l.TryGetProperty("fallback", out var fb)
                ? fb.GetString() ?? id : id;

            // Collect this question's raw values across all responses.
            var values = new List<string>();
            foreach (var ans in parsedAnswers)
            {
                if (!ans.TryGetProperty(id, out var v)) continue;
                switch (v.ValueKind)
                {
                    case JsonValueKind.Array:
                        values.AddRange(v.EnumerateArray()
                            .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() ?? "" : x.ToString())
                            .Where(s => s.Length > 0));
                        break;
                    case JsonValueKind.String:
                        var s = v.GetString() ?? "";
                        if (s.Trim().Length > 0) values.Add(s);
                        break;
                    case JsonValueKind.Number: values.Add(v.ToString()); break;
                    case JsonValueKind.True: values.Add("true"); break;
                    case JsonValueKind.False: values.Add("false"); break;
                }
            }

            // Option list for choice questions (toggle/consent get Yes/No).
            var options = new List<(string Value, string Label)>();
            if (item.TryGetProperty("props", out var props) && props.ValueKind == JsonValueKind.Object)
            {
                if (props.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                    foreach (var o in opts.EnumerateArray())
                    {
                        var ov = o.TryGetProperty("value", out var ovEl) ? ovEl.ToString() : "";
                        var ol = o.TryGetProperty("label", out var olEl)
                            ? (olEl.ValueKind == JsonValueKind.Object
                                && olEl.TryGetProperty("fallback", out var olFb) ? olFb.GetString() : olEl.GetString()) ?? ov
                            : ov;
                        options.Add((ov, ol));
                    }
                if (type is "toggle" or "consent" && options.Count == 0)
                {
                    var yes = props.TryGetProperty("trueLabel", out var tl) ? tl.GetString() : null;
                    var no = props.TryGetProperty("falseLabel", out var fl) ? fl.GetString() : null;
                    options.Add(("true", yes ?? "Yes"));
                    options.Add(("false", no ?? "No"));
                }
            }
            else if (type is "toggle" or "consent")
            {
                options.Add(("true", "Yes"));
                options.Add(("false", "No"));
            }

            var isChoice = options.Count > 0;
            var isText = TextTypes.Contains(type);

            double? average = null;
            var numeric = values
                .Select(v => double.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? (double?)d : null)
                .Where(d => d != null).Select(d => d!.Value).ToList();
            if (numeric.Count > 0 && numeric.Count == values.Count && (type == "number" || isChoice))
                average = Math.Round(numeric.Average(), 2);

            questions.Add(new
            {
                id,
                label,
                type,
                answered = values.Count,
                average,
                options = isChoice
                    ? options.Select(o => new
                    {
                        value = o.Value,
                        label = o.Label,
                        count = values.Count(v => v == o.Value),
                    }).ToList()
                    : null,
                texts = isText ? values.Take(500).ToList() : null,
            });
        }
        return questions;
    }

    /// <summary>Yields every item element of pages[].sections[].groups[].items[].</summary>
    private static IEnumerable<JsonElement> WalkItems(JsonElement root)
    {
        if (!root.TryGetProperty("pages", out var pages) || pages.ValueKind != JsonValueKind.Array)
            yield break;
        foreach (var page in pages.EnumerateArray())
        {
            if (!page.TryGetProperty("sections", out var sections) || sections.ValueKind != JsonValueKind.Array) continue;
            foreach (var section in sections.EnumerateArray())
            {
                if (!section.TryGetProperty("groups", out var groups) || groups.ValueKind != JsonValueKind.Array) continue;
                foreach (var group in groups.EnumerateArray())
                {
                    if (!group.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) continue;
                    foreach (var item in items.EnumerateArray())
                        yield return item;
                }
            }
        }
    }
}

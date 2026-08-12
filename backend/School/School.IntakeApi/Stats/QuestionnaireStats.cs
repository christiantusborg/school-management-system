using System.Text.Json;

namespace School.IntakeApi.Stats;

/// <summary>
/// Shared questionnaire statistics engine. Parses a questionnaire definition
/// into questions, maps a set of submissions onto per-question aggregates
/// (choice counts, numeric averages, free-text comments) across one or more
/// versions, and flags questions that changed or were removed between
/// versions. Used by both the intake-instance stats and the public-form stats
/// (single run + combined runs).
/// </summary>
public static class QuestionnaireStats
{
    public sealed record Question(string Id, string Type, string Label, List<(string Value, string Label)> Options);
    public sealed record VersionInfo(string Version, string Hash, List<Question> Questions);
    public sealed record Respondent(int N, DateTime? SubmittedAt, string? VersionHash, Dictionary<string, JsonElement> Answers);

    /// <summary>Owner-set "reference" field types. Filled per public form (not by
    /// the respondent) and stamped into every submission's answers.</summary>
    public static readonly string[] ReferenceTypes = { "refSchool", "refPartner", "refPartnerProgramme", "refText" };
    public static bool IsReferenceType(string? type) => type is not null && Array.IndexOf(ReferenceTypes, type) >= 0;

    /// <summary>Pull the reference fields (id, type, label) out of a questionnaire
    /// definition, in document order.</summary>
    public static List<(string Id, string Type, string Label)> ExtractReferenceFields(string? definitionJson)
    {
        var list = new List<(string, string, string)>();
        if (string.IsNullOrWhiteSpace(definitionJson)) return list;
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
                if (id is null || type is null || !IsReferenceType(type)) continue;
                var label = item.TryGetProperty("label", out var lEl) && lEl.TryGetProperty("fallback", out var fEl)
                    ? fEl.GetString() ?? id : id;
                list.Add((id, type, label));
            }
        }
        catch { /* malformed definition → no reference fields */ }
        return list;
    }

    public static List<Question> ParseQuestions(string definitionJson)
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
                // Per-field opt-out: props.showInStats === false hides an item
                // from the aggregates (used by owner-set reference fields when
                // the owner chooses "submissions only"). Absent/true = shown.
                if (item.TryGetProperty("props", out var pEl)
                    && pEl.TryGetProperty("showInStats", out var siEl)
                    && siEl.ValueKind == JsonValueKind.False) continue;
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

    public static Dictionary<string, JsonElement> ParseAnswers(string answersJson)
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

    public static string? AnswerToString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.ToString(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Array => string.Join(", ", el.EnumerateArray().Select(x => x.ToString())),
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => el.ToString(),
    };

    public sealed record Result(
        List<object> Questions,
        List<Question> Ordered,
        IReadOnlySet<string> ChangedIds,
        IReadOnlySet<string> RemovedIds);

    /// <summary>
    /// Build the ordered question list + per-question aggregates from the given
    /// versions (frozen first, current last) and respondents. Current version's
    /// questions come first; questions that only exist in older versions are
    /// appended and flagged removed; label drift between versions flags changed.
    /// </summary>
    public static Result Build(List<VersionInfo> versions, List<Respondent> respondents)
    {
        var current = versions.Count > 0 ? versions[^1] : new VersionInfo("1", "", []);

        var ordered = new List<Question>(current.Questions);
        var known = current.Questions.Select(q => q.Id).ToHashSet();
        var changed = new HashSet<string>();
        foreach (var v in versions.Take(Math.Max(0, versions.Count - 1)))
        foreach (var q in v.Questions)
        {
            if (!known.Add(q.Id))
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

            // Owner-set reference fields (refSchool/refPartner/…) carry no
            // predefined options — synthesize them from the observed values so
            // they aggregate as bars ("IBSS: 20") like any choice question,
            // which also makes combined-run compares work generically.
            var isRef = q.Type.StartsWith("ref", StringComparison.Ordinal);
            var effectiveOptions = q.Options;
            if (isRef && q.Options.Count == 0 && answered.Count > 0)
                effectiveOptions = answered.Distinct().Select(a => (Value: a!, Label: a!)).ToList();

            var isChoice = effectiveOptions.Count > 0;
            var isText = q.Type is "text" or "textarea";

            object? choices = null; double? average = null;
            if (isChoice)
            {
                var counts = effectiveOptions.Select(o => new
                {
                    value = o.Value,
                    label = o.Label,
                    count = answered.Count(a => a == o.Value || a == o.Label),
                }).ToList();
                var other = answered.Count - counts.Sum(c => c.count);
                choices = new { options = counts, other };
                if (effectiveOptions.All(o => double.TryParse(o.Value, out _)))
                {
                    var nums = answered.Select(a =>
                    {
                        if (double.TryParse(a, out var d)) return (double?)d;
                        var m = effectiveOptions.FirstOrDefault(o => o.Label == a);
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

            return (object)new
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

        return new Result(questionStats, ordered, changed, removedIds);
    }

    /// <summary>Project respondents to the anonymous per-answer shape used by the stats payloads.</summary>
    public static IEnumerable<object> ProjectRespondents(List<Respondent> respondents, List<Question> ordered, List<VersionInfo> versions) =>
        respondents.Select(r => new
        {
            respondent = r.N,
            submittedAt = r.SubmittedAt,
            version = versions.FirstOrDefault(v => v.Hash == r.VersionHash)?.Version ?? "?",
            answers = ordered.ToDictionary(q => q.Id, q =>
                r.Answers.TryGetValue(q.Id, out var v) ? AnswerToString(v) : null),
        });
}

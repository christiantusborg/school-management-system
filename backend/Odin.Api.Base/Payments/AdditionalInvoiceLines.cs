using System.Text.Json;

namespace Odin.Api.Base.Payments;

/// <summary>
/// (De)serializes the <c>LinesJson</c> column of an AdditionalInvoice:
/// a JSON array of {"text","amount"} objects. Tolerant parse — a malformed
/// value yields an empty list rather than an exception.
/// </summary>
public static class AdditionalInvoiceLines
{
    public sealed record Line(string Text, decimal Amount);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static List<Line> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<Line>>(json, Options) ?? []; }
        catch { return []; }
    }

    public static string Serialize(IEnumerable<Line> lines) =>
        JsonSerializer.Serialize(lines, Options);

    public static decimal Total(string? json) => Parse(json).Sum(l => l.Amount);

    /// <summary>Human-readable name for the invoice: its line texts joined
    /// ("Attestation fee + Delivery fee"), or a numbered fallback.</summary>
    public static string Title(string? json, int sequence)
    {
        var texts = Parse(json).Select(l => l.Text?.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        return texts.Count > 0 ? string.Join(" + ", texts) : $"Additional invoice {sequence}";
    }
}

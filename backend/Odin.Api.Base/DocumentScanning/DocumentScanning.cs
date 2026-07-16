using Polly;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Odin.Api.Base.DocumentScanning;

// ── Factory abstractions: swap the OCR engine or AI provider via config ──────

/// <summary>Extracts plain text from an uploaded document (image or PDF).</summary>
public interface IOcrEngine
{
    Task<string> ExtractTextAsync(byte[] bytes, string fileName, CancellationToken ct);
}

public sealed record AiValidationOutcome(string RawJson, decimal? Confidence, decimal? FraudRisk);

/// <summary>Validates a document from its OCR text using the type's AiPrompt.</summary>
public interface IAiDocumentValidator
{
    Task<AiValidationOutcome> ValidateAsync(string aiPrompt, string ocrText, CancellationToken ct);
}

/// <summary>
/// Tesseract CLI engine — the exact recipe from ~/projects/ocr/run.sh:
/// images go straight to tesseract; PDFs are rasterised page-by-page with
/// ghostscript first. Requires tesseract-ocr (+ language packs) and
/// ghostscript in the image. Language list configurable.
/// </summary>
public sealed class TesseractCliOcrEngine(string languages) : IOcrEngine
{
    public async Task<string> ExtractTextAsync(byte[] bytes, string fileName, CancellationToken ct)
    {
        var work = Directory.CreateTempSubdirectory("ocr-");
        try
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant().TrimStart('.');
            var input = Path.Combine(work.FullName, "input." + (ext.Length > 0 ? ext : "bin"));
            await File.WriteAllBytesAsync(input, bytes, ct);

            var pages = new List<string>();
            if (ext == "pdf")
            {
                await RunAsync("gs", $"-sDEVICE=png16m -r200 -dNOPAUSE -dBATCH -dQUIET -sOutputFile=\"{work.FullName}/p-%03d.png\" \"{input}\"", ct);
                pages.AddRange(Directory.GetFiles(work.FullName, "p-*.png").OrderBy(p => p));
            }
            else
            {
                pages.Add(input);
            }

            var sb = new StringBuilder();
            foreach (var page in pages)
            {
                var (stdout, _) = await RunAsync("tesseract", $"-l {languages} \"{page}\" -", ct);
                sb.AppendLine(stdout);
            }
            return sb.ToString().Trim();
        }
        finally
        {
            try { work.Delete(recursive: true); } catch { /* temp cleanup best effort */ }
        }
    }

    private static async Task<(string Stdout, string Stderr)> RunAsync(string cmd, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var stdout = await p.StandardOutput.ReadToEndAsync(ct);
        var stderr = await p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"{cmd} exited {p.ExitCode}: {stderr}");
        return (stdout, stderr);
    }
}

/// <summary>
/// Ollama validator — mirrors ~/projects/ocr/ocr_local.py: POST /api/generate
/// with stream=false and temperature 0, prompt = the document type's AiPrompt
/// plus the OCR text. Expects the standard JSON verdict back and reads the
/// 0.00–1.00 confidence / fraudRisk fields from it.
/// </summary>
public sealed class OllamaAiValidator(HttpClient http, string baseUrl, string model) : IAiDocumentValidator
{
    // Retry policy per spec: first wait 5 s, double each attempt, max 10
    // retries (5s, 10s, 20s … ~42 min worst case). Retries transient HTTP
    // failures and connection errors — e.g. Ollama busy or unreachable.
    private static readonly Polly.Retry.AsyncRetryPolicy Retry =
        Polly.Policy.Handle<HttpRequestException>()
            .Or<TaskCanceledException>(ex => ex.InnerException is TimeoutException)
            .WaitAndRetryAsync(10, attempt => TimeSpan.FromSeconds(5 * Math.Pow(2, attempt - 1)));

    public async Task<AiValidationOutcome> ValidateAsync(string aiPrompt, string ocrText, CancellationToken ct)
    {
        var prompt = aiPrompt
            + "\n\nDOCUMENT OCR TEXT (extracted with Tesseract; treat OCR noise gently but flag substantive anomalies):\n---\n"
            + ocrText + "\n---";
        var body = JsonSerializer.Serialize(new
        {
            model,
            prompt,
            stream = false,
            options = new { temperature = 0 },
        });
        var payload = await Retry.ExecuteAsync(async token =>
        {
            using var resp = await http.PostAsync(
                baseUrl.TrimEnd('/') + "/api/generate",
                new StringContent(body, Encoding.UTF8, "application/json"), token);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(token);
        }, ct);
        using var doc = JsonDocument.Parse(payload);
        var raw = doc.RootElement.GetProperty("response").GetString() ?? "";

        // Strip markdown fences if the model added them despite instructions.
        var json = raw.Trim();
        if (json.StartsWith("```"))
        {
            var open = json.IndexOf('\n');
            var close = json.LastIndexOf("```", StringComparison.Ordinal);
            if (open >= 0 && close > open) json = json[(open + 1)..close].Trim();
        }

        decimal? confidence = null, fraud = null;
        try
        {
            using var verdict = JsonDocument.Parse(json);
            var root = verdict.RootElement;
            if (root.TryGetProperty("confidence", out var c) && c.ValueKind == JsonValueKind.Number)
                confidence = Math.Clamp(c.GetDecimal(), 0m, 1m);
            if (root.TryGetProperty("fraudRisk", out var f) && f.ValueKind == JsonValueKind.Number)
                fraud = Math.Clamp(f.GetDecimal(), 0m, 1m);
            // Fallback for models that only filled the 0-100 fields.
            if (confidence is null && root.TryGetProperty("legitimacyConfidence", out var lc) && lc.ValueKind == JsonValueKind.Number)
                confidence = Math.Clamp(lc.GetDecimal() / 100m, 0m, 1m);
        }
        catch { /* keep raw output; scores stay null */ }

        return new AiValidationOutcome(json, confidence, fraud);
    }
}

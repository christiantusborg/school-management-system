using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using Odin.Api.Base.DocumentScanning;
using Odin.Api.Base.Storage;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

/// <summary>
/// Background scanner for UPLOADED student documents (system-generated
/// letters are skipped). Strictly sequential by design: one document at a
/// time — OCR first (Tesseract), then AI validation (Ollama) using the
/// document type's AiPrompt; stores OcrResult, AiResult and the 0.00–1.00
/// AiConfidence / AiFraudRisk scores. Engines come from the factory
/// registrations in Program.cs, so either can be swapped via config.
/// Disabled entirely unless DocumentScan:OllamaUrl is configured.
/// </summary>
internal sealed class DocumentScanWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration cfg,
    ILogger<DocumentScanWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var ollamaUrl = cfg["DocumentScan:OllamaUrl"];
        if (string.IsNullOrWhiteSpace(ollamaUrl))
        {
            logger.LogInformation("[DocScan] DocumentScan:OllamaUrl not configured — scanner idle.");
            return;
        }
        logger.LogInformation("[DocScan] Started (sequential). Ollama: {Url}", ollamaUrl);

        while (!ct.IsCancellationRequested)
        {
            var didWork = false;
            try
            {
                didWork = await ProcessNextAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DocScan] Cycle failed");
            }
            // One at a time; short pause between documents, longer when idle.
            await Task.Delay(TimeSpan.FromSeconds(didWork ? 2 : 30), ct);
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OdinDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var ocr = scope.ServiceProvider.GetRequiredService<IOcrEngine>();
        var ai = scope.ServiceProvider.GetRequiredService<IAiDocumentValidator>();

        // Uploaded documents only — never the letters our own system renders.
        var doc = await db.StudentDocuments
            .Where(d => d.DeletedAt == null
                && d.StoragePath != null
                && d.AiResult == null
                && !d.DocumentType.IsSystemGenerated)
            .OrderBy(d => d.UploadedAt)
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(ct);
        if (doc is null) return false;

        logger.LogInformation("[DocScan] Scanning {File} ({Type})", doc.FileName, doc.DocumentType.Name);
        try
        {
            if (string.IsNullOrWhiteSpace(doc.OcrResult))
            {
                await using var s = await storage.OpenReadAsync(doc.StoragePath!, ct);
                using var ms = new MemoryStream();
                await s.CopyToAsync(ms, ct);
                doc.OcrResult = await ocr.ExtractTextAsync(ms.ToArray(), doc.FileName, ct);
                await db.SaveChangesAsync(ct); // keep OCR even if the AI step fails
            }

            var prompt = string.IsNullOrWhiteSpace(doc.DocumentType.AiPrompt)
                ? $"Validate that this document is a \"{doc.DocumentType.Name}\" and answer in JSON with confidence and fraudRisk between 0.00 and 1.00."
                : doc.DocumentType.AiPrompt;
            var outcome = await ai.ValidateAsync(prompt, doc.OcrResult ?? "", ct);
            doc.AiResult = outcome.RawJson;
            doc.AiConfidence = outcome.Confidence;
            doc.AiFraudRisk = outcome.FraudRisk;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("[DocScan] Done {File}: confidence={C} fraudRisk={F}",
                doc.FileName, outcome.Confidence, outcome.FraudRisk);
        }
        catch (Exception ex)
        {
            // Persist the failure so the row is not retried forever; clearing
            // AiResult in the DB re-queues the document.
            doc.AiResult = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message, at = DateTime.UtcNow });
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogWarning(ex, "[DocScan] Failed on {File}", doc.FileName);
        }
        return true;
    }
}

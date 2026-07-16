using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace School.IntakeApi.Outputs;

/// <summary>
/// Generated artifacts (PDF/JSON/CSV) attached to an intake response. Core
/// generates client-side and uploads the finished file; IBSS keeps that
/// contract but stores the bytes via IFileStorage (like letters and student
/// documents) instead of core's encrypted case SharedFiles. Admin-only.
/// </summary>
[Route("/v1/intake/intake-responses/{intakeResponseId:guid}/outputs")]
[EndpointTag("Intake.Outputs")]
public sealed class IntakeOutputsV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/intake/intake-responses/{intakeResponseId:guid}/outputs", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/intake-responses/{intakeResponseId:guid}/outputs", UploadAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/intake-outputs/{intakeOutputId:guid}/file", DownloadAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/intake/intake-outputs/{intakeOutputId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private const int MaxOutputBytes = 50 * 1024 * 1024;

    private static IResult Ok(object data) => Results.Ok(new { success = true, data });
    private static IResult Fail(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { success = false, error }, statusCode: status);

    public sealed class UploadRequest
    {
        public string? FileName { get; init; }
        public string? ContentType { get; init; }
        public string? BytesBase64 { get; init; }
        public string? OutputKind { get; init; }
        public Guid? DocumentTemplateId { get; init; }
    }

    private static async Task<IResult> ListAsync(
        Guid intakeResponseId, OdinDbContext db, CancellationToken ct)
    {
        var items = await db.IntakeOutputs
            .Where(o => o.IntakeResponseId == intakeResponseId && o.DeletedAt == null)
            .OrderByDescending(o => o.GeneratedAt)
            .Select(o => new
            {
                intakeOutputId = o.IntakeOutputId,
                fileName = o.FileName,
                contentType = o.ContentType,
                outputKind = o.OutputKind.ToString(),
                sizeBytes = o.SizeBytes,
                generatedAt = o.GeneratedAt,
                documentTemplateId = o.DocumentTemplateId,
                templateName = o.DocumentTemplate != null ? o.DocumentTemplate.Name : null,
            })
            .ToListAsync(ct);
        return Ok(new { items });
    }

    private static async Task<IResult> UploadAsync(
        Guid intakeResponseId, [FromBody] UploadRequest body,
        OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var response = await db.IntakeResponses
            .FirstOrDefaultAsync(r => r.IntakeResponseId == intakeResponseId && r.DeletedAt == null, ct);
        if (response is null) return Fail("not_found", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(body.FileName) || string.IsNullOrWhiteSpace(body.BytesBase64))
            return Fail("file_required");
        if (!Enum.TryParse<IntakeOutputKind>(body.OutputKind, ignoreCase: true, out var kind))
            return Fail("output_kind_invalid");

        byte[] bytes;
        try { bytes = Convert.FromBase64String(body.BytesBase64); }
        catch { return Fail("bytes_not_base64"); }
        if (bytes.Length == 0 || bytes.Length > MaxOutputBytes) return Fail("file_too_large");

        string storagePath;
        using (var ms = new MemoryStream(bytes))
        {
            storagePath = await storage.SaveAsync(
                ms, $"intake-outputs/{intakeResponseId}/{Guid.NewGuid()}-{body.FileName.Trim()}", ct);
        }

        var output = new IntakeOutput
        {
            IntakeResponseId = intakeResponseId,
            DocumentTemplateId = body.DocumentTemplateId,
            OutputKind = kind,
            FileName = body.FileName.Trim(),
            ContentType = string.IsNullOrWhiteSpace(body.ContentType) ? "application/octet-stream" : body.ContentType,
            StoragePath = storagePath,
            SizeBytes = bytes.Length,
        };
        db.IntakeOutputs.Add(output);
        await db.SaveChangesAsync(ct);
        return Ok(new { intakeOutputId = output.IntakeOutputId, generatedAt = output.GeneratedAt });
    }

    private static async Task<IResult> DownloadAsync(
        Guid intakeOutputId, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var o = await db.IntakeOutputs
            .FirstOrDefaultAsync(x => x.IntakeOutputId == intakeOutputId && x.DeletedAt == null, ct);
        if (o is null) return Fail("not_found", StatusCodes.Status404NotFound);
        var stream = await storage.OpenReadAsync(o.StoragePath, ct);
        return Results.File(stream, o.ContentType, o.FileName);
    }

    private static async Task<IResult> DeleteAsync(
        Guid intakeOutputId, OdinDbContext db, CancellationToken ct)
    {
        var o = await db.IntakeOutputs
            .FirstOrDefaultAsync(x => x.IntakeOutputId == intakeOutputId && x.DeletedAt == null, ct);
        if (o is null) return Fail("not_found", StatusCodes.Status404NotFound);
        o.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }
}

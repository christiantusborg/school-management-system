using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace School.IntakeApi.PdfService;

/// <summary>
/// Thin authenticated proxy in front of the Python pdf-service container
/// (PyMuPDF): AcroForm field extraction and form fill. The SPA never talks
/// to the Python service directly — the proxy keeps it off the public
/// network (same pattern as QuVian core). Target URL from configuration
/// key "PdfService:BaseUrl".
/// </summary>
[Route("/v1/intake/pdf-service")]
[EndpointTag("Intake.PdfService")]
public sealed class PdfServiceProxyV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/intake/pdf-service/extract-fields", ExtractFieldsAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/pdf-service/fill", FillAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class ExtractRequest
    {
        public string? BytesBase64 { get; init; }
        public string? Filename { get; init; }
    }

    public sealed class FillRequest
    {
        public string? BytesBase64 { get; init; }
        public Dictionary<string, string>? Values { get; init; }
        public bool Flatten { get; init; }
    }

    private static IResult Fail(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { success = false, error }, statusCode: status);

    private static string BaseUrl(IConfiguration cfg) =>
        cfg["PdfService:BaseUrl"] ?? "http://localhost:8081";

    private static async Task<IResult> ExtractFieldsAsync(
        [FromBody] ExtractRequest body,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] IConfiguration cfg,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.BytesBase64)) return Fail("bytes_required");
        try { _ = Convert.FromBase64String(body.BytesBase64); }
        catch { return Fail("bytes_not_base64"); }

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        try
        {
            using var resp = await client.PostAsync(
                $"{BaseUrl(cfg)}/extract-fields",
                new StringContent(JsonSerializer.Serialize(new { bytesBase64 = body.BytesBase64 }),
                    Encoding.UTF8, "application/json"), ct);
            var payload = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return Fail($"pdf_service_error: {payload}", StatusCodes.Status502BadGateway);
            using var doc = JsonDocument.Parse(payload);
            return Results.Ok(new { success = true, data = doc.RootElement.Clone() });
        }
        catch (HttpRequestException ex)
        {
            return Fail($"pdf_service_unreachable: {ex.Message}", StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> FillAsync(
        [FromBody] FillRequest body,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] IConfiguration cfg,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.BytesBase64)) return Fail("bytes_required");
        try { _ = Convert.FromBase64String(body.BytesBase64); }
        catch { return Fail("bytes_not_base64"); }

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(120);
        try
        {
            using var resp = await client.PostAsync(
                $"{BaseUrl(cfg)}/fill",
                new StringContent(JsonSerializer.Serialize(new
                {
                    bytesBase64 = body.BytesBase64,
                    values = body.Values ?? new Dictionary<string, string>(),
                    flatten = body.Flatten,
                }), Encoding.UTF8, "application/json"), ct);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                return Fail($"pdf_service_error: {err}", StatusCodes.Status502BadGateway);
            }
            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            return Results.Ok(new { success = true, data = new { bytesBase64 = Convert.ToBase64String(bytes) } });
        }
        catch (HttpRequestException ex)
        {
            return Fail($"pdf_service_unreachable: {ex.Message}", StatusCodes.Status502BadGateway);
        }
    }
}

using System.Globalization;
using Odin.Api.Base.Letters;
using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.ProgrammeApi;

/// <summary>
/// Renders a letter template's current canvas state as a PDF without
/// touching <c>StudentDocuments</c>. Lets admins see exactly what a
/// release will look like before clicking Save &amp; Publish.
///
/// Mirrors <c>LetterReleaseService.ReleaseAsync</c>'s render pipeline but
/// (a) reads layout JSON from the request body — i.e. the in-flight canvas
/// the admin is editing, not the persisted row, (b) uses synthetic tag
/// values + a 5-row sample transcript so the preview works for any
/// programme, even one with no enrolments yet, (c) returns the bytes
/// inline as <c>application/pdf</c>.
/// </summary>
[Route("/v1/admin/programmes")]
[EndpointTag("Admin.LetterTemplates")]
public sealed class AdminProgrammesV1LetterTemplatePreviewEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/programmes/{programmeId:guid}/letter-templates/{type}/preview", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class PreviewRequest
    {
        public string? CertificateLayoutJson { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        OdinDbContext db,
        IFileStorage storage,
        LetterPdfRenderer renderer,
        Guid programmeId,
        string type,
        [FromBody] PreviewRequest body,
        CancellationToken ct)
    {
        if (!Enum.TryParse<LetterType>(type, ignoreCase: true, out var letterType))
            return Results.BadRequest(new { error = "unknown letter type" });

        var layout = CertificateLayout.TryParse(body?.CertificateLayoutJson);
        if (layout is null)
            return Results.BadRequest(new { error = "Layout JSON is empty or invalid — design something on the canvas first." });

        // Pre-fetch every asset the layout references (page backgrounds + image fields).
        // Same shape as LetterReleaseService — silently drop missing assets so a
        // half-configured layout still previews instead of 500-ing.
        var assetIds = LetterPdfRenderer.ExtractCertificateAssetIds(layout);
        var assets = new Dictionary<Guid, byte[]>();
        foreach (var id in assetIds)
        {
            var path = await db.LetterAssets
                .Where(a => a.LetterAssetId == id && a.DeletedAt == null)
                .Select(a => a.StoragePath)
                .FirstOrDefaultAsync(ct);
            if (path is null) continue;
            try
            {
                await using var s = await storage.OpenReadAsync(path, ct);
                using var ms = new MemoryStream();
                await s.CopyToAsync(ms, ct);
                if (ms.Length > 0) assets[id] = ms.ToArray();
            }
            catch (FileNotFoundException) { /* asset row exists but file is gone — skip */ }
        }

        var tags = BuildPreviewTags();

        // Only build a sample transcript when the layout actually contains a
        // transcriptTable field — saves work for offer/admission previews.
        IReadOnlyList<TranscriptGradeRow>? rows = null;
        var hasTranscriptTable = layout.GetPages()
            .Any(p => p.Fields?.Any(f => string.Equals(f.Kind, "transcriptTable", StringComparison.OrdinalIgnoreCase)) ?? false);
        if (hasTranscriptTable) rows = BuildSampleTranscript();

        var pdfBytes = renderer.RenderCertificate(layout, assets, tags, rows);

        var fileName = $"preview-{letterType}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        return Results.File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Synthetic tag values that mirror what <c>LetterTagResolver</c> would
    /// produce for a fully-populated enrolment, so previews show realistic
    /// letter content without depending on any database state.
    /// </summary>
    private static IReadOnlyDictionary<string, string> BuildPreviewTags()
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in LetterTagRegistry.All)
            tags[t.Token] = string.Empty;

        tags["[student full name]"]    = "Jane Doe";
        tags["[student firstname]"]    = "Jane";
        tags["[student surname]"]      = "Doe";
        tags["[student number]"]       = "STU-2026-0001";
        tags["[student address]"]      = "12 Sample Street, London, UK";
        tags["[passport id]"]          = "AB123456";
        tags["[date]"]                 = DateTime.UtcNow.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);
        tags["[date of birth]"]        = "15 March 1998";
        tags["[commencement date]"]    = "01 September 2026";
        tags["[completion date]"]      = "01 September 2027";
        tags["[graduation date]"]      = "01 September 2027";
        tags["[duration of study]"]    = "12 months";
        tags["[mode of study]"]        = "Full-time on-campus";
        tags["[instruction language]"] = "English";
        tags["[partner name]"]         = "Sample Partner Institute";
        tags["[program name]"]         = "Bachelor of Business Administration";
        tags["[specialization name]"]  = "Finance";
        tags["[grade]"]                = "78.5";
        tags["[ects achieved]"]        = "60";
        tags["[transcript]"]           = string.Empty; // transcript HTML token; the renderer uses transcriptTable fields for layout-based previews

        return tags;
    }

    private static IReadOnlyList<TranscriptGradeRow> BuildSampleTranscript() =>
    [
        new("FIN101", "Principles of Finance",     6, 82m),
        new("ACC102", "Financial Accounting",      6, 75m),
        new("ECO110", "Microeconomics",            6, 88m),
        new("MGT120", "Introduction to Management", 6, 70m),
        new("STA130", "Business Statistics",       6, 79m),
    ];
}

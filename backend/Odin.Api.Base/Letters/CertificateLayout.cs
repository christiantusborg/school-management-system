using System.Text.Json;
using System.Text.Json.Serialization;

namespace Odin.Api.Base.Letters;

/// <summary>
/// Persisted JSON shape for <see cref="SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.LetterTemplate.CertificateLayoutJson"/>.
/// One layout per programme. Fields render on top of the background image at
/// the absolute coordinates given (page space = pixels of the background).
/// </summary>
public sealed class CertificateLayout
{
    /// <summary>Legacy single-page background. Read-through via <see cref="GetPages"/> when Pages is empty.</summary>
    [JsonPropertyName("backgroundAssetId")]
    public Guid? BackgroundAssetId { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; } = 2000;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 1414;

    /// <summary>Output page size: "A4" | "A3" | "Letter" | "Custom".
    /// "Custom" maps the design canvas (Width × Height) directly to the
    /// PDF page; named sizes scale onto the corresponding standard sheet.</summary>
    [JsonPropertyName("pageSize")]
    public string PageSize { get; set; } = "A4";

    /// <summary>"portrait", "landscape", or "auto" (landscape iff Width &gt;= Height).</summary>
    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = "auto";

    [JsonPropertyName("marginTop")]    public int MarginTop    { get; set; }
    [JsonPropertyName("marginRight")]  public int MarginRight  { get; set; }
    [JsonPropertyName("marginBottom")] public int MarginBottom { get; set; }
    [JsonPropertyName("marginLeft")]   public int MarginLeft   { get; set; }

    /// <summary>Set by the seeder; lets later seed runs decide whether to
    /// overwrite previously-seeded layouts. Admin saves should preserve this
    /// value verbatim so an admin's edits aren't clobbered on the next boot.</summary>
    [JsonPropertyName("_seedFingerprint")]
    public int? SeedFingerprint { get; set; }

    /// <summary>Legacy single-page fields. Read-through via <see cref="GetPages"/>.</summary>
    [JsonPropertyName("fields")]
    public List<CertificateField> Fields { get; set; } = new();

    /// <summary>Multi-page layout. When non-empty, takes precedence over the
    /// top-level <see cref="Fields"/>/<see cref="BackgroundAssetId"/>.</summary>
    [JsonPropertyName("pages")]
    public List<CertificatePage> Pages { get; set; } = new();

    /// <summary>Returns the effective page list. Falls back to a synthetic
    /// single page built from the legacy top-level fields when Pages is empty.</summary>
    public IReadOnlyList<CertificatePage> GetPages()
    {
        if (Pages.Count > 0) return Pages;
        return new[]
        {
            new CertificatePage
            {
                BackgroundAssetId = BackgroundAssetId,
                Fields = Fields ?? new List<CertificateField>(),
            },
        };
    }

    public static CertificateLayout? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<CertificateLayout>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };
}

public sealed class CertificatePage
{
    [JsonPropertyName("backgroundAssetId")]
    public Guid? BackgroundAssetId { get; set; }

    [JsonPropertyName("fields")]
    public List<CertificateField> Fields { get; set; } = new();
}

public sealed class CertificateField
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

    /// <summary>"text" (default) or "image". Image fields overlay an asset
    /// from the archive at (x, y) sized (width × height); text fields render
    /// the resolved tag value with font/color/align.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "text";

    /// <summary>Tag token (e.g. "[student full name]"). Null for static fields.</summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    /// <summary>Static text for fields without a tag.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Optional HTML body for static fields. When set, the renderer
    /// parses inline tags (&lt;strong&gt;, &lt;em&gt;, &lt;br&gt;, &lt;p&gt;, &lt;ul&gt;/&lt;ol&gt;/&lt;li&gt;)
    /// and emits per-span formatting on top of the field-level font/color.
    /// Plain <see cref="Text"/> is used as the fallback (and as the value the
    /// Konva canvas renders, since it can't display HTML).</summary>
    [JsonPropertyName("htmlText")]
    public string? HtmlText { get; set; }

    [JsonPropertyName("prefix")]
    public string? Prefix { get; set; }

    [JsonPropertyName("suffix")]
    public string? Suffix { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("fontSize")]
    public int FontSize { get; set; } = 24;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#000000";

    [JsonPropertyName("align")]
    public string Align { get; set; } = "left"; // "left" | "center" | "right"

    [JsonPropertyName("bold")]
    public bool Bold { get; set; }

    [JsonPropertyName("italic")]
    public bool Italic { get; set; }

    /// <summary>Asset id for image fields (kind == "image").</summary>
    [JsonPropertyName("imageAssetId")]
    public Guid? ImageAssetId { get; set; }

    /// <summary>Render width in page space for image fields.</summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>Render height in page space for image fields.</summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }
}

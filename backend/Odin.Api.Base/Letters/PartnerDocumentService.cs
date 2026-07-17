using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using Odin.Api.Base.Storage;

namespace Odin.Api.Base.Letters;

/// <summary>One fill-out field of a partner document type.</summary>
public sealed record PartnerDocField(string Id, string Label, string Type, string? Source)
{
    /// <summary>The tag token this field is placed with in the designer.</summary>
    public string Token => "[" + Label.Trim().ToLowerInvariant() + "]";
}

/// <summary>
/// Renders partner documents (certificates, authorization letters, diplomas…)
/// from their globally configured type: ONE template per type (same editor
/// stack as student letters) + per-document field values. Text/date/partner
/// fields substitute as [tags]; image fields replace bound image placeholders
/// with the uploaded asset. PDFs are always rendered live so they pick up the
/// current design and partner data.
/// </summary>
public sealed class PartnerDocumentService(
    OdinDbContext db,
    IFileStorage storage,
    LetterPdfRenderer renderer)
{
    public const string TextField = "text";
    public const string DateField = "date";
    public const string ImageField = "image";
    public const string PartnerField = "partner";

    /// <summary>Partner-profile values a "partner" field can auto-fill from.</summary>
    public static readonly (string Key, string Label)[] PartnerSources =
    [
        ("name", "Partner name"),
        ("website", "Website"),
        ("registrationNumber", "Registration no."),
        ("taxId", "Tax ID"),
        ("contactEmail", "Contact email"),
        ("contactPhone", "Contact phone"),
        ("address", "Address"),
        ("contractStart", "Contract start date"),
        ("contractEnd", "Contract end date"),
    ];

    public static List<PartnerDocField> ParseFields(string? fieldsJson)
    {
        var result = new List<PartnerDocField>();
        if (string.IsNullOrWhiteSpace(fieldsJson)) return result;
        try
        {
            using var doc = JsonDocument.Parse(fieldsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var label = el.TryGetProperty("label", out var l) ? l.GetString() : null;
                if (string.IsNullOrWhiteSpace(label)) continue;
                var id = el.TryGetProperty("id", out var i) ? i.GetString() : null;
                var type = el.TryGetProperty("type", out var t) ? t.GetString() : TextField;
                var source = el.TryGetProperty("source", out var s) ? s.GetString() : null;
                result.Add(new PartnerDocField(
                    string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id,
                    label.Trim(),
                    type is TextField or DateField or ImageField or PartnerField ? type : TextField,
                    source));
            }
        }
        catch (JsonException) { /* tolerate hand-broken JSON — no fields */ }
        return result;
    }

    public static string SerializeFields(IEnumerable<PartnerDocField> fields) =>
        JsonSerializer.Serialize(fields.Select(f => new { id = f.Id, label = f.Label, type = f.Type, source = f.Source }));

    public static Dictionary<string, string> ParseValues(string? valuesJson)
    {
        if (string.IsNullOrWhiteSpace(valuesJson)) return new Dictionary<string, string>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(valuesJson)
                ?.Where(kv => kv.Value is not null)
                .ToDictionary(kv => kv.Key, kv => kv.Value!) ?? new Dictionary<string, string>();
        }
        catch (JsonException) { return new Dictionary<string, string>(); }
    }

    private static string FormatDate(DateTime d) => d.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>Tag values for one document: base partner tags + custom fields.</summary>
    public async Task<Dictionary<string, string>> ResolveTagsAsync(
        Guid partnerId, IReadOnlyList<PartnerDocField> fields,
        IReadOnlyDictionary<string, string> values, CancellationToken ct)
    {
        var partner = await db.Partners
            .Where(p => p.PartnerId == partnerId)
            .Select(p => new { p.Name, p.Website, p.RegistrationNumber, p.TaxId })
            .FirstOrDefaultAsync(ct);
        var contactEmail = await db.PartnerContactEmails
            .Where(e => e.PartnerId == partnerId && e.DeletedAt == null)
            .OrderByDescending(e => e.IsPrimary).ThenBy(e => e.PartnerContactEmailId)
            .Select(e => e.Email).FirstOrDefaultAsync(ct);
        var contactPhone = await db.PartnerContactPhones
            .Where(p => p.PartnerId == partnerId && p.DeletedAt == null)
            .OrderByDescending(p => p.IsPrimary).ThenBy(p => p.PartnerContactPhoneId)
            .Select(p => p.Phone).FirstOrDefaultAsync(ct);
        var address = await db.PartnerAddresses
            .Where(a => a.PartnerId == partnerId && a.DeletedAt == null)
            .OrderBy(a => a.PartnerAddressId)
            .Select(a => new { a.Line1, a.Line2, a.City, a.StateRegion, a.PostalCode, a.CountryCode })
            .FirstOrDefaultAsync(ct);
        // Most-recent contract, same pick as the partner Profile tab shows.
        var contract = await db.PartnerContracts
            .Where(c => c.PartnerId == partnerId && c.DeletedAt == null)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new { c.StartDate, EndDate = (DateTime?)c.EndDate })
            .FirstOrDefaultAsync(ct);

        var addressText = address is null
            ? string.Empty
            : string.Join(", ", new[] { address.Line1, address.Line2, address.City, address.StateRegion, address.PostalCode, address.CountryCode }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        string ResolveSource(string? source) => source switch
        {
            "name" => partner?.Name ?? string.Empty,
            "website" => partner?.Website ?? string.Empty,
            "registrationNumber" => partner?.RegistrationNumber ?? string.Empty,
            "taxId" => partner?.TaxId ?? string.Empty,
            "contactEmail" => contactEmail ?? string.Empty,
            "contactPhone" => contactPhone ?? string.Empty,
            "address" => addressText,
            "contractStart" => contract is { } c1 ? FormatDate(c1.StartDate) : string.Empty,
            "contractEnd" => contract?.EndDate is { } ce ? FormatDate(ce) : string.Empty,
            _ => string.Empty,
        };

        var tags = new Dictionary<string, string>
        {
            ["[partner name]"] = partner?.Name ?? string.Empty,
            ["[date]"] = FormatDate(DateTime.UtcNow),
            ["[valid until]"] = contract?.EndDate is { } end ? FormatDate(end) : "-",
        };

        foreach (var f in fields)
        {
            var value = f.Type switch
            {
                PartnerField => ResolveSource(f.Source),
                DateField => values.TryGetValue(f.Id, out var dv)
                    && DateTime.TryParseExact(dv, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                    ? FormatDate(d)
                    : values.GetValueOrDefault(f.Id, string.Empty),
                ImageField => string.Empty, // handled as an image override, not a text tag
                _ => values.GetValueOrDefault(f.Id, string.Empty),
            };
            tags[f.Token] = value;
        }
        return tags;
    }

    /// <summary>Renders one partner document from its type's template.</summary>
    public async Task<byte[]?> RenderDocumentAsync(Guid partnerDocumentId, CancellationToken ct)
    {
        var doc = await db.PartnerDocuments
            .Where(d => d.PartnerDocumentId == partnerDocumentId && d.DeletedAt == null)
            .Select(d => new
            {
                d.PartnerId,
                d.FieldValuesJson,
                Type = db.PartnerDocumentTypes
                    .Where(t => t.PartnerDocumentTypeId == d.PartnerDocumentTypeId)
                    .Select(t => new { t.FieldsJson, t.LayoutJson })
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);
        if (doc?.Type is null) return null;

        var layout = CertificateLayout.TryParse(doc.Type.LayoutJson)
            ?? CertificateLayout.TryParse(DefaultLayoutJson())!;
        var fields = ParseFields(doc.Type.FieldsJson);
        var values = ParseValues(doc.FieldValuesJson);
        var tags = await ResolveTagsAsync(doc.PartnerId, fields, values, ct);

        // Image fields: a template image placeholder bound to "[field]" is
        // replaced by the asset uploaded on THIS document.
        var imageByToken = fields
            .Where(f => f.Type == ImageField)
            .GroupBy(f => f.Token)
            .ToDictionary(g => g.Key, g => g.First().Id);
        foreach (var page in layout.Pages ?? [])
        foreach (var field in page.Fields ?? [])
        {
            if (field.Kind != "image" || string.IsNullOrWhiteSpace(field.Tag)) continue;
            if (imageByToken.TryGetValue(field.Tag, out var fieldId)
                && values.TryGetValue(fieldId, out var raw)
                && Guid.TryParse(raw, out var assetId))
            {
                field.ImageAssetId = assetId;
            }
        }

        return await RenderWithAssetsAsync(layout, tags, ct);
    }

    /// <summary>
    /// Renders a type's template for the config designer preview: sample
    /// values for the base tags, literal tokens for the custom fields so the
    /// designer sees where each field lands.
    /// </summary>
    public async Task<byte[]?> RenderTypePreviewAsync(
        Guid partnerDocumentTypeId, string? layoutJsonOverride, CancellationToken ct)
    {
        var type = await db.PartnerDocumentTypes
            .Where(t => t.PartnerDocumentTypeId == partnerDocumentTypeId && t.DeletedAt == null)
            .Select(t => new { t.FieldsJson, t.LayoutJson })
            .FirstOrDefaultAsync(ct);
        if (type is null) return null;

        var layout = CertificateLayout.TryParse(layoutJsonOverride ?? type.LayoutJson)
            ?? CertificateLayout.TryParse(DefaultLayoutJson())!;

        var tags = new Dictionary<string, string>
        {
            ["[partner name]"] = "Sample Partner Institute",
            ["[date]"] = FormatDate(DateTime.UtcNow),
            ["[valid until]"] = FormatDate(DateTime.UtcNow.AddYears(2)),
        };
        foreach (var f in ParseFields(type.FieldsJson))
            if (f.Type != ImageField)
                tags[f.Token] = f.Token; // literal token = visible placeholder

        return await RenderWithAssetsAsync(layout, tags, ct);
    }

    private async Task<byte[]> RenderWithAssetsAsync(
        CertificateLayout layout, Dictionary<string, string> tags, CancellationToken ct)
    {
        var assets = new Dictionary<Guid, byte[]>();
        foreach (var id in LetterPdfRenderer.ExtractCertificateAssetIds(layout).Distinct())
        {
            var path = await db.LetterAssets
                .Where(a => a.LetterAssetId == id && a.DeletedAt == null)
                .Select(a => a.StoragePath)
                .FirstOrDefaultAsync(ct);
            if (path is null) continue;
            try
            {
                using var s = await storage.OpenReadAsync(path, ct);
                using var ms = new MemoryStream();
                await s.CopyToAsync(ms, ct);
                if (ms.Length > 0) assets[id] = ms.ToArray();
            }
            catch (FileNotFoundException) { /* asset row exists but blob gone — skip */ }
        }
        return renderer.RenderCertificate(layout, assets, tags);
    }

    /// <summary>
    /// Starter design for a "Partnership Authorization Letter" type — an A4
    /// portrait text letter modelled on the "Letter of Authorization -
    /// Approved Admissions Partner" sample. Static text may embed [tag]
    /// tokens inline; the renderer substitutes them at render time.
    /// </summary>
    public static string AuthorizationLetterLayoutJson()
    {
        const int leftX = 140;
        const int contentW = 1134; // 1414 - 2 × 140
        var layout = new CertificateLayout
        {
            Width = 1414,
            Height = 2000,
            PageSize = "A4",
            Orientation = "portrait",
            Pages =
            [
                new CertificatePage
                {
                    Fields =
                    [
                        new CertificateField { Kind = "text", Tag = "[school name]", X = 0, Y = 110, FontSize = 40, Color = "#1a2d4f", Align = "center", Bold = true, Width = 1414 },
                        new CertificateField { Kind = "text", Tag = "[date]", X = 0, Y = 200, FontSize = 24, Color = "#333333", Align = "center", Width = 1414 },
                        new CertificateField { Kind = "text", Text = "Letter of Authorization - Approved Admissions Partner", X = 0, Y = 320, FontSize = 34, Color = "#111111", Align = "center", Bold = true, Width = 1414 },
                        new CertificateField { Kind = "text", Text = "To Whom It May Concern", X = 0, Y = 430, FontSize = 28, Color = "#111111", Align = "center", Width = 1414 },
                        new CertificateField { Kind = "text", Text = "This is to formally certify that [partner name] has been appointed as an Approved Admissions Partner of [school name].", X = leftX, Y = 540, FontSize = 26, Color = "#222222", Align = "left", Width = contentW },
                        new CertificateField { Kind = "text", Text = "As an Approved Admissions Partner, the above-mentioned institution is granted the rights and responsibilities to promote the academic programs of [school name] and support the recruitment of prospective students for various academic programs, including Doctorate, Master's, Bachelor's, Certification's and Executive Education programs, in accordance with the guidelines and admission standards established by the institution. Under this authorization, the partner is entrusted with the following responsibilities:", X = leftX, Y = 660, FontSize = 26, Color = "#222222", Align = "left", Width = contentW },
                        new CertificateField { Kind = "text", Text = "1. Student Recruitment", X = leftX, Y = 960, FontSize = 26, Color = "#111111", Align = "left", Bold = true, Width = contentW },
                        new CertificateField { Kind = "text", Text = "Identify, guide and support prospective students through the admission process in line with the institution's requirements.", X = leftX, Y = 1010, FontSize = 26, Color = "#222222", Align = "left", Width = contentW },
                        new CertificateField { Kind = "text", Text = "2. Programme Promotion", X = leftX, Y = 1110, FontSize = 26, Color = "#111111", Align = "left", Bold = true, Width = contentW },
                        new CertificateField { Kind = "text", Text = "Represent the institution's academic programs accurately and professionally in all marketing and counselling activities.", X = leftX, Y = 1160, FontSize = 26, Color = "#222222", Align = "left", Width = contentW },
                        new CertificateField { Kind = "text", Text = "3. Compliance", X = leftX, Y = 1260, FontSize = 26, Color = "#111111", Align = "left", Bold = true, Width = contentW },
                        new CertificateField { Kind = "text", Text = "Adhere to the admission standards, quality guidelines and code of conduct established by the institution at all times.", X = leftX, Y = 1310, FontSize = 26, Color = "#222222", Align = "left", Width = contentW },
                        new CertificateField { Kind = "text", Text = "This authorization remains valid while the partnership agreement between the parties is in force.", X = leftX, Y = 1440, FontSize = 26, Color = "#222222", Align = "left", Width = contentW },
                        new CertificateField { Kind = "text", Text = "____________________________", X = leftX, Y = 1650, FontSize = 28, Color = "#222222", Align = "left" },
                        new CertificateField { Kind = "text", Text = "Signature", X = leftX, Y = 1710, FontSize = 26, Color = "#222222", Align = "left", Bold = true },
                        new CertificateField { Kind = "text", Text = "Founder & CEO", X = leftX, Y = 1760, FontSize = 24, Color = "#555555", Align = "left" },
                    ],
                },
            ],
        };
        return JsonSerializer.Serialize(layout, CertificateLayout.JsonOpts);
    }

    /// <summary>
    /// Starter design for a "Certificate of Partnership" type: CERTIFICATE OF
    /// PARTNERSHIP heading, "proudly presented to", the partner name, an
    /// honour line, date issued, Valid Until (contract end) and a signature
    /// block. Landscape A4. All editable in the certificate editor.
    /// </summary>
    public static string DefaultLayoutJson()
    {
        var layout = new CertificateLayout
        {
            Width = 2000,
            Height = 1414,
            PageSize = "A4",
            Orientation = "landscape",
            Pages =
            [
                new CertificatePage
                {
                    Fields =
                    [
                        new CertificateField { Kind = "text", Tag = "[school name]", X = 0, Y = 90, FontSize = 34, Color = "#b08d2f", Align = "center", Bold = true, Width = 2000 },
                        new CertificateField { Kind = "text", Text = "CERTIFICATE", X = 0, Y = 220, FontSize = 140, Color = "#2e4b3f", Align = "center", Bold = true, Width = 2000 },
                        new CertificateField { Kind = "text", Text = "OF PARTNERSHIP", X = 0, Y = 400, FontSize = 54, Color = "#2e4b3f", Align = "center", Width = 2000 },
                        new CertificateField { Kind = "text", Text = "This certificate is proudly presented to:", X = 0, Y = 540, FontSize = 38, Color = "#222222", Align = "center", Width = 2000 },
                        new CertificateField { Kind = "text", Tag = "[partner name]", X = 0, Y = 640, FontSize = 84, Color = "#2e4b3f", Align = "center", Italic = true, Width = 2000 },
                        new CertificateField { Kind = "text", Text = "We honor and celebrate the strength of our collaboration and look forward to achieving greater heights in education, research and community engagement.", X = 300, Y = 810, FontSize = 30, Color = "#333333", Align = "center", Width = 1400 },
                        new CertificateField { Kind = "text", Tag = "[date]", Prefix = "Date Issued: ", X = 160, Y = 1120, FontSize = 28, Color = "#222222", Align = "left" },
                        new CertificateField { Kind = "text", Tag = "[valid until]", Prefix = "Valid Until: ", X = 160, Y = 1180, FontSize = 28, Color = "#222222", Align = "left" },
                        new CertificateField { Kind = "text", Text = "____________________________", X = 1250, Y = 1090, FontSize = 30, Color = "#222222", Align = "center", Width = 600 },
                        new CertificateField { Kind = "text", Text = "Signature", X = 1250, Y = 1150, FontSize = 28, Color = "#222222", Align = "center", Bold = true, Width = 600 },
                        new CertificateField { Kind = "text", Text = "Founder & CEO", X = 1250, Y = 1200, FontSize = 24, Color = "#555555", Align = "center", Width = 600 },
                    ],
                },
            ],
        };
        return JsonSerializer.Serialize(layout, CertificateLayout.JsonOpts);
    }
}

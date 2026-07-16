using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using Odin.Api.Base.Storage;

namespace Odin.Api.Base.Letters;

/// <summary>
/// Renders a partner's certificate of cooperation (per (partner, school)) from
/// its stored layout — same editor/renderer stack as student letters, but the
/// tag context is the partner, not an enrolment. PDFs are always rendered live
/// so they pick up the current design and partner name.
/// </summary>
public sealed class PartnerCertificateService(
    OdinDbContext db,
    IFileStorage storage,
    LetterPdfRenderer renderer)
{
    /// <summary>Tag values available on a partner certificate.</summary>
    public async Task<Dictionary<string, string>> ResolveTagsAsync(
        Guid partnerId, Guid schoolId, CancellationToken ct)
    {
        var partnerName = await db.Partners
            .Where(p => p.PartnerId == partnerId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;
        var schoolName = await db.Schools
            .Where(s => s.SchoolId == schoolId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;
        var today = DateTime.UtcNow.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);
        return new Dictionary<string, string>
        {
            ["[partner name]"] = partnerName,
            ["[school name]"] = schoolName,
            ["[date]"] = today,
        };
    }

    /// <summary>
    /// Renders the certificate PDF. <paramref name="layoutJsonOverride"/> lets
    /// the editor preview unsaved edits. Returns null when the certificate
    /// doesn't exist (or is deleted).
    /// </summary>
    public async Task<byte[]?> RenderAsync(
        Guid partnerCertificateId, CancellationToken ct, string? layoutJsonOverride = null)
    {
        var cert = await db.PartnerCertificates
            .Where(c => c.PartnerCertificateId == partnerCertificateId && c.DeletedAt == null)
            .Select(c => new { c.PartnerId, c.SchoolId, c.CertificateLayoutJson })
            .FirstOrDefaultAsync(ct);
        if (cert is null) return null;

        var layout = CertificateLayout.TryParse(layoutJsonOverride ?? cert.CertificateLayoutJson)
            ?? CertificateLayout.TryParse(DefaultLayoutJson())!;

        var tags = await ResolveTagsAsync(cert.PartnerId, cert.SchoolId, ct);

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
    /// Starter design for a new partner certificate, echoing the sample:
    /// CERTIFICATE OF PARTNERSHIP heading, "proudly presented to", the partner
    /// name, an honour line, date issued and a signature block. Landscape A4.
    /// All of it is editable/replaceable in the certificate editor.
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
                        new CertificateField { Kind = "text", Text = "Valid Until: ", X = 160, Y = 1180, FontSize = 28, Color = "#222222", Align = "left" },
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

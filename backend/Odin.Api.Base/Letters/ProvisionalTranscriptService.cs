using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Data;
using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Letters;

/// <summary>
/// Renders a <b>provisional</b> transcript PDF for an enrolment from the grades
/// saved so far, watermarked so it can't be mistaken for the official one. It
/// uses the same per-(programme, partner) published Transcript template as a
/// real release but does NOT persist a <see cref="SharedLibrary.Basics.Opaque.Domains.StudentDocument"/>
/// — it just returns the bytes for an inline download. Lets students/partners
/// preview the transcript mid-grading, before Admission approves the grades.
/// </summary>
public sealed class ProvisionalTranscriptService(
    OdinDbContext db,
    IFileStorage storage,
    LetterTagResolver tagResolver,
    LetterPdfRenderer renderer,
    ILogger<ProvisionalTranscriptService> logger)
{
    public const string Watermark = "PROVISIONAL";

    /// <summary>
    /// Returns the watermarked transcript bytes, or null when there is no
    /// published Transcript template for the enrolment's (programme, partner).
    /// </summary>
    public async Task<byte[]?> RenderAsync(Guid enrollmentId, CancellationToken ct)
    {
        var enrollment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId && e.DeletedAt == null)
            .Select(e => new
            {
                e.PartnerId,
                ProgrammeId = db.Specializations
                    .Where(s => s.SpecializationId == e.SpecializationId)
                    .Select(s => s.ProgrammeId)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);
        if (enrollment is null) return null;

        var template = await db.LetterTemplates.FirstOrDefaultAsync(t =>
            t.ProgrammeId == enrollment.ProgrammeId &&
            t.PartnerId == enrollment.PartnerId &&
            t.LetterType == LetterType.Transcript &&
            t.DeletedAt == null, ct);
        if (template is null || !template.IsPublished)
        {
            logger.LogInformation(
                "[ProvisionalTranscript] No published transcript template for programme {ProgrammeId} partner {PartnerId}",
                enrollment.ProgrammeId, enrollment.PartnerId);
            return null;
        }

        async Task<Dictionary<Guid, byte[]>> ReadAssetsAsync(IEnumerable<Guid> ids)
        {
            var dict = new Dictionary<Guid, byte[]>();
            foreach (var id in ids.Distinct())
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
                    if (ms.Length > 0) dict[id] = ms.ToArray();
                }
                catch (FileNotFoundException) { /* asset row exists but blob gone — skip */ }
            }
            return dict;
        }

        var layout = CertificateLayout.TryParse(template.CertificateLayoutJson);
        if (layout is null)
        {
            logger.LogWarning("[ProvisionalTranscript] Template {Id} has no layout to render", template.LetterTemplateId);
            return null;
        }

        var tags = await tagResolver.ResolveAsync(enrollmentId, ct, reference: null);
        var rows = await tagResolver.ResolveTranscriptRowsAsync(enrollmentId, ct);
        var assets = await ReadAssetsAsync(LetterPdfRenderer.ExtractCertificateAssetIds(layout));
        return renderer.RenderCertificate(layout, assets, tags, rows, watermark: Watermark);
    }
}

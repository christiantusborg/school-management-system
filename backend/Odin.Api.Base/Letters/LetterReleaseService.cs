using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Data;
using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Letters;

/// <summary>
/// Renders a letter PDF for an enrollment and saves it as a
/// <see cref="StudentDocument"/> backed by <see cref="IFileStorage"/>.
/// Phase 1 supports the HTML-based letter types (Offer / Admission /
/// Transcript). Certificate rendering arrives with the Konva editor in
/// Phase 4.
/// </summary>
public sealed class LetterReleaseService(
    OdinDbContext db,
    IFileStorage storage,
    LetterTagResolver tagResolver,
    LetterPdfRenderer renderer,
    ILogger<LetterReleaseService> logger)
{
    public async Task<Guid?> ReleaseAsync(
        Guid enrollmentId,
        LetterType letterType,
        CancellationToken ct)
    {
        var enrollment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => new
            {
                e.StudentEnrollmentId,
                e.StudentId,
                e.SpecializationId,
                ProgrammeId = db.Specializations
                    .Where(s => s.SpecializationId == e.SpecializationId)
                    .Select(s => s.ProgrammeId)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        if (enrollment is null)
        {
            logger.LogWarning("[Letters] Enrollment {EnrollmentId} not found", enrollmentId);
            return null;
        }

        var template = await db.LetterTemplates
            .FirstOrDefaultAsync(t =>
                t.ProgrammeId == enrollment.ProgrammeId &&
                t.LetterType == letterType &&
                t.DeletedAt == null, ct);

        if (template is null)
        {
            logger.LogWarning("[Letters] No template for programme {ProgrammeId} type {LetterType}",
                enrollment.ProgrammeId, letterType);
            return null;
        }

        // Gate release on the admin having explicitly published this template.
        // Seeded defaults sit in the DB as a starting canvas for the editor;
        // they only become release-eligible when an admin saves them.
        if (!template.IsPublished)
        {
            logger.LogInformation(
                "[Letters] Skipping {LetterType} for programme {ProgrammeId}: template not published.",
                letterType, enrollment.ProgrammeId);
            return null;
        }

        async Task<byte[]> ReadAssetAsync(Guid id)
        {
            var path = await db.LetterAssets
                .Where(a => a.LetterAssetId == id && a.DeletedAt == null)
                .Select(a => a.StoragePath)
                .FirstOrDefaultAsync(ct);
            if (path is null) return Array.Empty<byte>();
            using var s = await storage.OpenReadAsync(path, ct);
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms, ct);
            return ms.ToArray();
        }

        async Task<Dictionary<Guid, byte[]>> ReadAssetsAsync(IEnumerable<Guid> ids)
        {
            var dict = new Dictionary<Guid, byte[]>();
            foreach (var id in ids.Distinct())
            {
                var b = await ReadAssetAsync(id);
                if (b.Length > 0) dict[id] = b;
            }
            return dict;
        }

        // All letter types now author via the Konva layout editor. Storage:
        // CertificateLayoutJson (preferred) and BodyHtml (legacy fallback).
        // For each release, prefer the layout; fall back to the HTML body if
        // the admin hasn't authored a layout yet for this letter.
        var layout = CertificateLayout.TryParse(template.CertificateLayoutJson);
        byte[] pdfBytes;
        if (layout is not null)
        {
            var tags = await tagResolver.ResolveAsync(enrollmentId, ct);
            var assets = await ReadAssetsAsync(LetterPdfRenderer.ExtractCertificateAssetIds(layout));
            // Only fetch transcript rows if a layout actually contains a
            // transcriptTable field — saves a round-trip on offer/admission
            // letters that don't need the grade data.
            IReadOnlyList<TranscriptGradeRow>? rows = null;
            var hasTranscriptTable = layout.GetPages()
                .Any(p => p.Fields?.Any(f =>
                    string.Equals(f.Kind, "transcriptTable", StringComparison.OrdinalIgnoreCase)) ?? false);
            if (hasTranscriptTable)
                rows = await tagResolver.ResolveTranscriptRowsAsync(enrollmentId, ct);
            pdfBytes = renderer.RenderCertificate(layout, assets, tags, rows);
        }
        else if (!string.IsNullOrWhiteSpace(template.BodyHtml))
        {
            var tags = await tagResolver.ResolveAsync(enrollmentId, ct);
            var pages = TryParseHtmlPages(template.BodyHtml);
            var assets = await ReadAssetsAsync(LetterPdfRenderer.ExtractAssetIds(pages));
            pdfBytes = renderer.RenderHtml(pages, tags, assets);
        }
        else
        {
            logger.LogWarning("[Letters] Template {LetterTemplateId} has neither layout nor body", template.LetterTemplateId);
            return null;
        }

        var fileName = $"{letterType}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        string storagePath;
        using (var ms = new MemoryStream(pdfBytes))
        {
            storagePath = await storage.SaveAsync(
                ms,
                $"letters/{enrollment.StudentId}/{enrollmentId}/{Guid.NewGuid()}-{fileName}",
                ct);
        }

        var documentTypeId = letterType switch
        {
            LetterType.OfferLetter            => SystemDocumentTypeIds.OfferLetter,
            LetterType.AdmissionLetter        => SystemDocumentTypeIds.AdmissionLetter,
            LetterType.Transcript             => SystemDocumentTypeIds.Transcript,
            LetterType.Certificate            => SystemDocumentTypeIds.Certificate,
            LetterType.ProvisionalCertificate => SystemDocumentTypeIds.ProvisionalCertificate,
            _ => throw new ArgumentOutOfRangeException(nameof(letterType)),
        };

        var document = new StudentDocument
        {
            StudentDocumentId = Guid.NewGuid(),
            StudentId = enrollment.StudentId,
            EnrollmentId = enrollmentId,
            DocumentTypeId = documentTypeId,
            FileName = fileName,
            MimeType = "application/pdf",
            UploadedAt = DateTime.UtcNow,
            StoragePath = storagePath,
            CurrentStatusId = DocumentStatusIds.VerifiedByEnrolment,
        };
        db.StudentDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Letters] Released {LetterType} for enrollment {EnrollmentId} → {StudentDocumentId}",
            letterType, enrollmentId, document.StudentDocumentId);

        return document.StudentDocumentId;
    }

    private static IReadOnlyList<string> TryParseHtmlPages(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return Array.Empty<string>();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return doc.RootElement.EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
        }
        catch { /* not JSON, treat as legacy single-page */ }
        return new[] { body };
    }
}

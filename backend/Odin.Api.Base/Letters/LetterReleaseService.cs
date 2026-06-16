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
    LetterEmailService letterEmail,
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
                e.PartnerId,
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

        // Templates are per (programme, partner, letter type). Resolve the
        // partner from the enrolment; no cross-partner fallback, so a partner
        // that hasn't authored this letter simply releases nothing.
        var template = await db.LetterTemplates
            .FirstOrDefaultAsync(t =>
                t.ProgrammeId == enrollment.ProgrammeId &&
                t.PartnerId == enrollment.PartnerId &&
                t.LetterType == letterType &&
                t.DeletedAt == null, ct);

        if (template is null)
        {
            logger.LogWarning("[Letters] No template for programme {ProgrammeId} partner {PartnerId} type {LetterType}",
                enrollment.ProgrammeId, enrollment.PartnerId, letterType);
            return null;
        }

        // Ensure a stable reference code for this enrolment. Generated once on
        // the first release (first 8 hex chars of a GUID) and reused for every
        // letter type and every regeneration after, so a printed reference
        // never changes. Saved with the document below in the same SaveChanges.
        var enrollmentEntity = await db.Enrollments
            .FirstAsync(e => e.StudentEnrollmentId == enrollmentId, ct);
        if (string.IsNullOrEmpty(enrollmentEntity.LetterReferenceCode))
            enrollmentEntity.LetterReferenceCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var reference = $"IBSS-{LetterTypeCode(letterType)}-{enrollmentEntity.LetterReferenceCode}";

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
            var tags = await tagResolver.ResolveAsync(enrollmentId, ct, reference);
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
            var tags = await tagResolver.ResolveAsync(enrollmentId, ct, reference);
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

        // A unique index allows only one active document per
        // (Enrollment, DocumentType). On regeneration the letter already
        // exists, so update that row in place rather than inserting a
        // duplicate (which violates the index). The document id stays stable
        // across regenerations, keeping existing download links valid; the
        // superseded PDF blob is left in storage.
        var existing = await db.StudentDocuments
            .FirstOrDefaultAsync(d => d.EnrollmentId == enrollmentId
                && d.DocumentTypeId == documentTypeId
                && d.DeletedAt == null, ct);

        Guid resultId;
        if (existing is not null)
        {
            existing.FileName = fileName;
            existing.MimeType = "application/pdf";
            existing.UploadedAt = DateTime.UtcNow;
            existing.StoragePath = storagePath;
            existing.CurrentStatusId = DocumentStatusIds.VerifiedByEnrolment;
            resultId = existing.StudentDocumentId;
        }
        else
        {
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
            resultId = document.StudentDocumentId;
        }
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Letters] Released {LetterType} for enrollment {EnrollmentId} → {StudentDocumentId} ({Mode})",
            letterType, enrollmentId, resultId, existing is null ? "new" : "regenerated");

        // Auto-send the accompanying email (offer/admission only) when the
        // admin enabled it for this programme. Best-effort: a send failure
        // logs but never rolls back or fails the release.
        if (letterType is LetterType.OfferLetter or LetterType.AdmissionLetter)
        {
            try
            {
                var emailResult = await letterEmail.SendForLetterAsync(
                    enrollmentId, letterType, adHocCc: null, adHocBcc: null, requireEnabled: true, ct);
                if (emailResult.Outcome == LetterEmailOutcome.Sent)
                    logger.LogInformation("[Letters] Auto-emailed {LetterType} for enrolment {EnrollmentId} to {To}",
                        letterType, enrollmentId, emailResult.To);
                else if (emailResult.Outcome != LetterEmailOutcome.Disabled)
                    logger.LogInformation("[Letters] Auto-email not sent for {LetterType} enrolment {EnrollmentId}: {Outcome}",
                        letterType, enrollmentId, emailResult.Outcome);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Letters] Auto-email failed for {LetterType} enrolment {EnrollmentId}", letterType, enrollmentId);
            }
        }

        return resultId;
    }

    /// <summary>
    /// Short code embedded in a letter reference (<c>IBSS-{code}-{enrolment}</c>).
    /// Also used by the verify endpoint to report which letter type a scanned
    /// reference belongs to.
    /// </summary>
    public static string LetterTypeCode(LetterType letterType) => letterType switch
    {
        LetterType.OfferLetter            => "OL",
        LetterType.AdmissionLetter        => "AL",
        LetterType.Transcript             => "TR",
        LetterType.Certificate            => "CERT",
        LetterType.ProvisionalCertificate => "PCERT",
        _ => "DOC",
    };

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

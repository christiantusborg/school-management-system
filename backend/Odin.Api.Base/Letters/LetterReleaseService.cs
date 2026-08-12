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
    /// <summary>
    /// Fixed definition id of each built-in letter's dynamic twin (created by
    /// the Phase 1 enum→dynamic migration). Cutover routes every built-in
    /// release through its twin so all letters share one version-tracked
    /// pipeline. The twin carries the same template, DocumentType and
    /// reference prefix, so the released PDF is identical.
    /// </summary>
    public static Guid EnumTwinDefinitionId(LetterType t) => t switch
    {
        LetterType.OfferLetter            => Guid.Parse("22222222-2222-2222-2222-def000000001"),
        LetterType.AdmissionLetter        => Guid.Parse("22222222-2222-2222-2222-def000000002"),
        LetterType.Transcript             => Guid.Parse("22222222-2222-2222-2222-def000000003"),
        LetterType.Certificate            => Guid.Parse("22222222-2222-2222-2222-def000000004"),
        LetterType.ProvisionalCertificate => Guid.Parse("22222222-2222-2222-2222-def000000005"),
        LetterType.PrintableTranscript    => Guid.Parse("22222222-2222-2222-2222-def000000006"),
        LetterType.StudentIdCard          => Guid.Parse("22222222-2222-2222-2222-def000000007"),
        LetterType.FinalProposalApproval  => Guid.Parse("22222222-2222-2222-2222-def000000008"),
        LetterType.FinalProjectApproval   => Guid.Parse("22222222-2222-2222-2222-def000000009"),
        _ => Guid.Empty,
    };

    /// <summary>
    /// Release a built-in letter. Since the enum→dynamic cutover this simply
    /// delegates to the letter's dynamic twin: same template, same DocumentType
    /// (so it lands in the same StudentDocuments slot), same MGW-&lt;code&gt;
    /// reference, plus version history and definition-based email. The
    /// letterType hint keeps tag resolution exactly as before.
    /// </summary>
    public async Task<Guid?> ReleaseAsync(
        Guid enrollmentId,
        LetterType letterType,
        CancellationToken ct)
    {
        var defId = EnumTwinDefinitionId(letterType);
        if (defId == Guid.Empty)
        {
            logger.LogWarning("[Letters] No dynamic twin mapped for {LetterType}", letterType);
            return null;
        }
        return await ReleaseDynamicAsync(
            enrollmentId, defId, language: null, trigger: "System",
            generatedByName: "System", generatedByUserId: null,
            letterTypeHint: letterType, ct);
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
        LetterType.PrintableTranscript    => "PTR",
        LetterType.StudentIdCard          => "IDCARD",
        LetterType.FinalProposalApproval  => "PROPAPP",
        LetterType.FinalProjectApproval   => "PROJAPP",
        _ => "DOC",
    };

    private async Task<byte[]> ReadAssetAsync(Guid id, CancellationToken ct)
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

    private async Task<Dictionary<Guid, byte[]>> ReadAssetsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var dict = new Dictionary<Guid, byte[]>();
        foreach (var id in ids.Distinct())
        {
            var b = await ReadAssetAsync(id, ct);
            if (b.Length > 0) dict[id] = b;
        }
        return dict;
    }

    /// <summary>
    /// Renders a template to PDF bytes — the shared core of the enum-letter
    /// and config-created (dynamic) release paths. All letter types author
    /// via the Konva layout editor: CertificateLayoutJson preferred, BodyHtml
    /// as the legacy fallback. Returns null when the template has neither.
    /// </summary>
    private async Task<byte[]?> RenderTemplatePdfAsync(
        LetterTemplate template, Guid studentId, Guid enrollmentId,
        string reference, LetterType? letterType, CancellationToken ct)
    {
        var layout = CertificateLayout.TryParse(template.CertificateLayoutJson);
        // A layout with no fields and no background on any page would render a
        // blank page. Treat that as "not designed for this programme/partner
        // yet" and fall through (to BodyHtml, else skip) so a blank PDF is
        // never filed as the student's document.
        var layoutHasContent = layout is not null && layout.GetPages()
            .Any(p => (p.Fields?.Count ?? 0) > 0 || p.BackgroundAssetId is not null);
        if (layout is not null && layoutHasContent)
        {
            var tags = await tagResolver.ResolveAsync(enrollmentId, ct, reference, letterType);
            var assets = await ReadAssetsAsync(LetterPdfRenderer.ExtractCertificateAssetIds(layout), ct);
            // Virtual student-photo asset: image fields pointing at the
            // sentinel id get the student's uploaded Student Card Picture.
            if (template.CertificateLayoutJson?.Contains(SystemLetterAssetIds.StudentPhoto.ToString()) == true)
            {
                var photoPath = await db.StudentDocuments
                    .Where(d => d.StudentId == studentId
                        && d.DocumentTypeId == SystemDocumentTypeIds.StudentCardPicture
                        && d.DeletedAt == null)
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => d.StoragePath)
                    .FirstOrDefaultAsync(ct);
                if (photoPath is not null)
                {
                    try
                    {
                        await using var ps = await storage.OpenReadAsync(photoPath, ct);
                        using var pms = new MemoryStream();
                        await ps.CopyToAsync(pms, ct);
                        if (pms.Length > 0) assets[SystemLetterAssetIds.StudentPhoto] = pms.ToArray();
                    }
                    catch { /* no photo → the artwork placeholder stays visible */ }
                }
            }
            // Only fetch transcript rows if a layout actually contains a
            // transcriptTable field — saves a round-trip on letters that
            // don't need the grade data.
            IReadOnlyList<TranscriptGradeRow>? rows = null;
            var hasTranscriptTable = layout.GetPages()
                .Any(p => p.Fields?.Any(f =>
                    string.Equals(f.Kind, "transcriptTable", StringComparison.OrdinalIgnoreCase)) ?? false);
            if (hasTranscriptTable)
                rows = await tagResolver.ResolveTranscriptRowsAsync(enrollmentId, ct);
            return renderer.RenderCertificate(layout, assets, tags, rows);
        }
        if (!string.IsNullOrWhiteSpace(template.BodyHtml))
        {
            var tags = await tagResolver.ResolveAsync(enrollmentId, ct, reference, letterType);
            var pages = TryParseHtmlPages(template.BodyHtml);
            var assets = await ReadAssetsAsync(LetterPdfRenderer.ExtractAssetIds(pages), ct);
            return renderer.RenderHtml(pages, tags, assets);
        }
        logger.LogWarning(
            "[Letters] Template {LetterTemplateId} has no drawable content (empty layout and no body) — skipping to avoid a blank PDF.",
            template.LetterTemplateId);
        return null;
    }

    /// <summary>
    /// Releases a config-created (dynamic) letter type for an enrolment.
    /// Template lookup is per (programme, partner, definition, language) with
    /// fallback to the English default (null language) when the requested
    /// language has no published version. Every successful render also
    /// appends a StudentDocumentVersion row — the audit/history trail the
    /// built-in letters don't have yet.
    /// </summary>
    public async Task<Guid?> ReleaseDynamicAsync(
        Guid enrollmentId, Guid letterTypeDefinitionId, string? language,
        string trigger, string? generatedByName, string? generatedByUserId,
        LetterType? letterTypeHint,
        CancellationToken ct)
    {
        var definition = await db.LetterTypeDefinitions
            .FirstOrDefaultAsync(d => d.LetterTypeDefinitionId == letterTypeDefinitionId && d.DeletedAt == null, ct);
        if (definition is null) return null;

        var enrollment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => new
            {
                e.StudentId,
                e.PartnerId,
                ProgrammeId = db.Specializations
                    .Where(s => s.SpecializationId == e.SpecializationId)
                    .Select(s => s.ProgrammeId)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);
        if (enrollment is null) return null;

        var lang = string.IsNullOrWhiteSpace(language) ? null : language.Trim();
        var template = await db.LetterTemplates.FirstOrDefaultAsync(t =>
            t.ProgrammeId == enrollment.ProgrammeId &&
            t.PartnerId == enrollment.PartnerId &&
            t.LetterTypeDefinitionId == letterTypeDefinitionId &&
            t.Language == lang &&
            t.IsPublished &&
            t.DeletedAt == null, ct);
        if (template is null && lang is not null)
        {
            // English default fallback: a missing translation never blocks.
            lang = null;
            template = await db.LetterTemplates.FirstOrDefaultAsync(t =>
                t.ProgrammeId == enrollment.ProgrammeId &&
                t.PartnerId == enrollment.PartnerId &&
                t.LetterTypeDefinitionId == letterTypeDefinitionId &&
                t.Language == null &&
                t.IsPublished &&
                t.DeletedAt == null, ct);
        }
        if (template is null)
        {
            logger.LogInformation("[Letters] No published template for definition {DefinitionId} programme {ProgrammeId} partner {PartnerId}",
                letterTypeDefinitionId, enrollment.ProgrammeId, enrollment.PartnerId);
            return null;
        }

        var enrollmentEntity = await db.Enrollments
            .FirstAsync(e => e.StudentEnrollmentId == enrollmentId, ct);
        if (string.IsNullOrEmpty(enrollmentEntity.LetterReferenceCode))
            enrollmentEntity.LetterReferenceCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var reference = $"MGW-{definition.ReferencePrefix}-{enrollmentEntity.LetterReferenceCode}";

        // letterTypeHint is set when a built-in letter is delegated here at
        // cutover, so tag resolution stays exactly as the enum path produced
        // (e.g. proposal vs project grade). Null for genuinely dynamic types.
        var pdfBytes = await RenderTemplatePdfAsync(template, enrollment.StudentId, enrollmentId, reference, letterTypeHint, ct);
        if (pdfBytes is null) return null;

        var safeType = new string(definition.Name.Where(char.IsLetterOrDigit).ToArray());
        var fileName = $"{(safeType.Length > 0 ? safeType : "Letter")}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        string storagePath;
        using (var ms = new MemoryStream(pdfBytes))
        {
            storagePath = await storage.SaveAsync(
                ms, $"letters/{enrollment.StudentId}/{enrollmentId}/{Guid.NewGuid()}-{fileName}", ct);
        }

        // Same one-active-document-per-(enrolment, type) contract as the enum
        // letters: the live row is always the LATEST render (stable id and
        // download links); the version rows below carry the history.
        var existing = await db.StudentDocuments
            .FirstOrDefaultAsync(d => d.EnrollmentId == enrollmentId
                && d.DocumentTypeId == definition.DocumentTypeId
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
                DocumentTypeId = definition.DocumentTypeId,
                FileName = fileName,
                MimeType = "application/pdf",
                UploadedAt = DateTime.UtcNow,
                StoragePath = storagePath,
                CurrentStatusId = DocumentStatusIds.VerifiedByEnrolment,
            };
            db.StudentDocuments.Add(document);
            resultId = document.StudentDocumentId;
        }

        var lastVersion = await db.StudentDocumentVersions
            .Where(v => v.StudentDocumentId == resultId)
            .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0;
        db.StudentDocumentVersions.Add(new StudentDocumentVersion
        {
            StudentDocumentId = resultId,
            VersionNumber = lastVersion + 1,
            FileName = fileName,
            StoragePath = storagePath,
            Trigger = trigger,
            GeneratedByName = generatedByName,
            GeneratedByUserId = generatedByUserId,
            Language = lang,
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Letters] Released dynamic '{Name}' v{Version} for enrollment {EnrollmentId} → {StudentDocumentId} ({Trigger})",
            definition.Name, lastVersion + 1, enrollmentId, resultId, trigger);

        // Auto-email on release: needs BOTH the type's "Email letter on
        // release" switch AND the programme email template's enable switch.
        // Best-effort — a send failure never rolls back the release.
        if (definition.EmailOnRelease)
        {
            try
            {
                var emailResult = await letterEmail.SendForDynamicLetterAsync(
                    enrollmentId, definition.LetterTypeDefinitionId,
                    adHocCc: null, adHocBcc: null, additionalText: null, requireEnabled: true, ct);
                if (emailResult.Outcome == LetterEmailOutcome.Sent)
                    logger.LogInformation("[Letters] Auto-emailed dynamic '{Name}' for enrolment {EnrollmentId} to {To}",
                        definition.Name, enrollmentId, emailResult.To);
                else if (emailResult.Outcome != LetterEmailOutcome.Disabled)
                    logger.LogInformation("[Letters] Auto-email not sent for dynamic '{Name}' enrolment {EnrollmentId}: {Outcome}",
                        definition.Name, enrollmentId, emailResult.Outcome);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Letters] Auto-email failed for dynamic '{Name}' enrolment {EnrollmentId}", definition.Name, enrollmentId);
            }
        }
        return resultId;
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

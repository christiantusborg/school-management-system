using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Data;
using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Letters;

public enum LetterEmailOutcome { Sent, NoTemplate, Disabled, NoRecipient, NoLetterFile, Failed }

public sealed record LetterEmailRecipient(string Email, bool Enabled);

public sealed record LetterEmailResult(
    LetterEmailOutcome Outcome,
    string? To = null,
    IReadOnlyList<string>? Cc = null,
    IReadOnlyList<string>? Bcc = null,
    string? Error = null);

/// <summary>
/// Composes and sends the email that accompanies a released letter: resolves
/// the per-(programme, type) <see cref="LetterEmailTemplate"/>, fills tags in
/// the subject/body, attaches the most recent released PDF, and dispatches via
/// <see cref="IEmailSender"/> to the student (To) plus the template's enabled
/// CC/BCC and any ad-hoc addresses. Only Offer and Admission letters are
/// emailable today.
/// </summary>
public sealed class LetterEmailService(
    OdinDbContext db,
    LetterTagResolver tagResolver,
    IFileStorage storage,
    IEmailSender emailSender,
    ILogger<LetterEmailService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<LetterEmailResult> SendForLetterAsync(
        Guid enrollmentId,
        LetterType letterType,
        IEnumerable<string>? adHocCc,
        IEnumerable<string>? adHocBcc,
        bool requireEnabled,
        CancellationToken ct)
    {
        if (letterType is not (LetterType.OfferLetter or LetterType.AdmissionLetter))
            return new LetterEmailResult(LetterEmailOutcome.NoTemplate, Error: "Only Offer and Admission letters are emailable.");

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
                SchoolId = db.Specializations
                    .Where(s => s.SpecializationId == e.SpecializationId)
                    .Select(s => s.Programmes.SchoolId)
                    .FirstOrDefault(),
                StudentEmail = db.Students
                    .Where(s => s.StudentId == e.StudentId)
                    .Select(s => s.User.Email)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        if (enrollment is null)
            return new LetterEmailResult(LetterEmailOutcome.NoTemplate, Error: "Enrolment not found.");

        // Per (programme, partner, letter type); partner comes from the
        // enrolment. No fallback: a partner with no template sends nothing.
        var template = await db.LetterEmailTemplates.FirstOrDefaultAsync(t =>
            t.ProgrammeId == enrollment.ProgrammeId &&
            t.PartnerId == enrollment.PartnerId &&
            t.LetterType == letterType &&
            t.LetterTypeDefinitionId == null &&
            t.DeletedAt == null, ct);

        if (template is null || string.IsNullOrWhiteSpace(template.Subject) || string.IsNullOrWhiteSpace(template.BodyHtml))
            return new LetterEmailResult(LetterEmailOutcome.NoTemplate, Error: "No email template authored for this programme's letter.");
        if (requireEnabled && !template.IsEmailEnabled)
            return new LetterEmailResult(LetterEmailOutcome.Disabled);

        if (string.IsNullOrWhiteSpace(enrollment.StudentEmail))
            return new LetterEmailResult(LetterEmailOutcome.NoRecipient, Error: "Student has no email address.");

        // Resolve tags for subject + body.
        var tags = await tagResolver.ResolveAsync(enrollmentId, ct);
        var subject = ApplyTags(template.Subject!, tags);
        var bodyHtml = ApplyTags(template.BodyHtml!, tags);

        // Attach the most recent released PDF for this (enrolment, type).
        var documentTypeId = letterType == LetterType.OfferLetter
            ? SystemDocumentTypeIds.OfferLetter
            : SystemDocumentTypeIds.AdmissionLetter;
        var doc = await db.StudentDocuments
            .Where(d => d.EnrollmentId == enrollmentId && d.DocumentTypeId == documentTypeId && d.DeletedAt == null)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new { d.FileName, d.StoragePath })
            .FirstOrDefaultAsync(ct);
        if (doc?.StoragePath is null)
            return new LetterEmailResult(LetterEmailOutcome.NoLetterFile, Error: "No released letter PDF found to attach.");

        byte[] pdfBytes;
        using (var s = await storage.OpenReadAsync(doc.StoragePath, ct))
        using (var ms = new MemoryStream())
        {
            await s.CopyToAsync(ms, ct);
            pdfBytes = ms.ToArray();
        }

        var cc = MergeRecipients(template.CcRecipientsJson, adHocCc);
        var bcc = MergeRecipients(template.BccRecipientsJson, adHocBcc);

        // From is resolved by the transport from the mail settings (per-school
        // when the programme's school has its own, else System Config → Email);
        // leave it unset here so the portal stays the single source of truth.
        var message = new EmailMessage(
            To: enrollment.StudentEmail!,
            Subject: subject,
            HtmlBody: bodyHtml,
            Cc: cc,
            Bcc: bcc,
            Attachments: new[] { new EmailAttachment(doc.FileName, "application/pdf", pdfBytes) },
            SchoolId: enrollment.SchoolId);

        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[LetterEmail] Send failed for enrolment {EnrollmentId} {LetterType}", enrollmentId, letterType);
            return new LetterEmailResult(LetterEmailOutcome.Failed, enrollment.StudentEmail, cc, bcc, ex.Message);
        }

        logger.LogInformation("[LetterEmail] Sent {LetterType} email for enrolment {EnrollmentId} to {To} (cc {CcCount}, bcc {BccCount})",
            letterType, enrollmentId, enrollment.StudentEmail, cc.Count, bcc.Count);
        return new LetterEmailResult(LetterEmailOutcome.Sent, enrollment.StudentEmail, cc, bcc);
    }

    /// <summary>
    /// Composes (without sending) the email for a config-created letter type:
    /// the programme+partner template with tags resolved and the optional
    /// [additional text] placeholder filled. Used by the preview endpoint and
    /// by <see cref="SendForDynamicLetterAsync"/> so what you preview is what
    /// sends.
    /// </summary>
    public async Task<(LetterEmailResult? Fail, string Subject, string BodyHtml, string To, List<string> Cc, List<string> Bcc, bool IsEnabled)>
        ComposeDynamicAsync(
            Guid enrollmentId, Guid letterTypeDefinitionId,
            IEnumerable<string>? adHocCc, IEnumerable<string>? adHocBcc,
            string? additionalText, CancellationToken ct)
    {
        var enrollment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => new
            {
                e.PartnerId,
                ProgrammeId = db.Specializations
                    .Where(s => s.SpecializationId == e.SpecializationId)
                    .Select(s => s.ProgrammeId)
                    .FirstOrDefault(),
                StudentEmail = db.Students
                    .Where(s => s.StudentId == e.StudentId)
                    .Select(s => s.User.Email)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);
        if (enrollment is null)
            return (new LetterEmailResult(LetterEmailOutcome.NoTemplate, Error: "Enrolment not found."), "", "", "", [], [], false);

        var template = await db.LetterEmailTemplates.FirstOrDefaultAsync(t =>
            t.ProgrammeId == enrollment.ProgrammeId &&
            t.PartnerId == enrollment.PartnerId &&
            t.LetterTypeDefinitionId == letterTypeDefinitionId &&
            t.DeletedAt == null, ct);
        if (template is null || string.IsNullOrWhiteSpace(template.Subject) || string.IsNullOrWhiteSpace(template.BodyHtml))
            return (new LetterEmailResult(LetterEmailOutcome.NoTemplate, Error: "No email template authored for this letter (programme + partner)."), "", "", "", [], [], false);
        if (string.IsNullOrWhiteSpace(enrollment.StudentEmail))
            return (new LetterEmailResult(LetterEmailOutcome.NoRecipient, Error: "Student has no email address."), "", "", "", [], [], false);

        var tags = await tagResolver.ResolveAsync(enrollmentId, ct);
        var subject = ApplyAdditionalText(ApplyTags(template.Subject!, tags), additionalText, htmlBody: false);
        var bodyHtml = ApplyAdditionalText(ApplyTags(template.BodyHtml!, tags), additionalText, htmlBody: true);
        var cc = MergeRecipients(template.CcRecipientsJson, adHocCc);
        var bcc = MergeRecipients(template.BccRecipientsJson, adHocBcc);
        return (null, subject, bodyHtml, enrollment.StudentEmail!, cc, bcc, template.IsEmailEnabled);
    }

    /// <summary>
    /// Sends the email for a config-created letter type with the latest
    /// released PDF attached. additionalText fills the [additional text]
    /// placeholder (appended as a closing paragraph when the template has no
    /// placeholder). requireEnabled = true is the auto-send path (status
    /// trigger / release): it silently reports Disabled unless the admin
    /// flipped the template's enable switch.
    /// </summary>
    public async Task<LetterEmailResult> SendForDynamicLetterAsync(
        Guid enrollmentId, Guid letterTypeDefinitionId,
        IEnumerable<string>? adHocCc, IEnumerable<string>? adHocBcc,
        string? additionalText, bool requireEnabled, CancellationToken ct)
    {
        var (fail, subject, bodyHtml, to, cc, bcc, isEnabled) = await ComposeDynamicAsync(
            enrollmentId, letterTypeDefinitionId, adHocCc, adHocBcc, additionalText, ct);
        if (fail is not null) return fail;
        if (requireEnabled && !isEnabled) return new LetterEmailResult(LetterEmailOutcome.Disabled);

        var meta = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => new
            {
                SchoolId = db.Specializations
                    .Where(s => s.SpecializationId == e.SpecializationId)
                    .Select(s => s.Programmes.SchoolId)
                    .FirstOrDefault(),
                DocumentTypeId = db.LetterTypeDefinitions
                    .Where(d => d.LetterTypeDefinitionId == letterTypeDefinitionId)
                    .Select(d => (Guid?)d.DocumentTypeId)
                    .FirstOrDefault(),
            })
            .FirstAsync(ct);
        if (meta.DocumentTypeId is null)
            return new LetterEmailResult(LetterEmailOutcome.NoTemplate, Error: "Unknown letter type.");

        var doc = await db.StudentDocuments
            .Where(d => d.EnrollmentId == enrollmentId && d.DocumentTypeId == meta.DocumentTypeId && d.DeletedAt == null)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new { d.FileName, d.StoragePath })
            .FirstOrDefaultAsync(ct);
        if (doc?.StoragePath is null)
            return new LetterEmailResult(LetterEmailOutcome.NoLetterFile, Error: "No released letter PDF found to attach.");

        byte[] pdfBytes;
        using (var s = await storage.OpenReadAsync(doc.StoragePath, ct))
        using (var ms = new MemoryStream())
        {
            await s.CopyToAsync(ms, ct);
            pdfBytes = ms.ToArray();
        }

        var message = new EmailMessage(
            To: to, Subject: subject, HtmlBody: bodyHtml, Cc: cc, Bcc: bcc,
            Attachments: new[] { new EmailAttachment(doc.FileName, "application/pdf", pdfBytes) },
            SchoolId: meta.SchoolId);
        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[LetterEmail] Dynamic send failed for enrolment {EnrollmentId} definition {DefinitionId}",
                enrollmentId, letterTypeDefinitionId);
            return new LetterEmailResult(LetterEmailOutcome.Failed, to, cc, bcc, ex.Message);
        }
        logger.LogInformation("[LetterEmail] Sent dynamic letter email for enrolment {EnrollmentId} definition {DefinitionId} to {To}",
            enrollmentId, letterTypeDefinitionId, to);
        return new LetterEmailResult(LetterEmailOutcome.Sent, to, cc, bcc);
    }

    /// <summary>
    /// Fills the [additional text] placeholder. The typed text is plain text:
    /// HTML-escaped, newlines become line breaks in HTML bodies. No
    /// placeholder + non-empty text → appended as a closing paragraph
    /// (HTML bodies only). Empty text → the placeholder renders as nothing.
    /// </summary>
    private static string ApplyAdditionalText(string content, string? additionalText, bool htmlBody)
    {
        const string token = "[additional text]";
        var text = (additionalText ?? string.Empty).Trim();
        var safe = System.Net.WebUtility.HtmlEncode(text);
        var value = htmlBody ? safe.Replace("\n", "<br>") : text;
        if (content.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            // Single-pass regex replace: never re-scans inserted text, so a
            // typed literal "[additional text]" can't loop.
            return System.Text.RegularExpressions.Regex.Replace(
                content,
                System.Text.RegularExpressions.Regex.Escape(token),
                value.Replace("$", "$$"),
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        if (htmlBody && value.Length > 0)
            return $"{content}<p>{value}</p>";
        return content;
    }

    private static string ApplyTags(string template, IReadOnlyDictionary<string, string> tags)
    {
        var result = template;
        foreach (var (token, value) in tags)
            result = result.Replace(token, value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    private static List<string> MergeRecipients(string? json, IEnumerable<string>? adHoc)
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<LetterEmailRecipient>>(json, JsonOpts);
                if (parsed is not null)
                    list.AddRange(parsed.Where(r => r.Enabled && !string.IsNullOrWhiteSpace(r.Email)).Select(r => r.Email.Trim()));
            }
            catch { /* malformed list: fall through with whatever parsed */ }
        }
        if (adHoc is not null)
            list.AddRange(adHoc.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()));
        // De-dupe case-insensitively, preserve order.
        return list
            .GroupBy(e => e.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();
    }
}

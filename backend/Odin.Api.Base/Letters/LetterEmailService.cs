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
            return new LetterEmailResult(LetterEmailOutcome.NoTemplate, Error: "Enrolment not found.");

        var template = await db.LetterEmailTemplates.FirstOrDefaultAsync(t =>
            t.ProgrammeId == enrollment.ProgrammeId &&
            t.LetterType == letterType &&
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

        // From is resolved by the transport from the admin's mail settings
        // (System Config → Email); leave it unset here so the portal stays the
        // single source of truth.
        var message = new EmailMessage(
            To: enrollment.StudentEmail!,
            Subject: subject,
            HtmlBody: bodyHtml,
            Cc: cc,
            Bcc: bcc,
            Attachments: new[] { new EmailAttachment(doc.FileName, "application/pdf", pdfBytes) });

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

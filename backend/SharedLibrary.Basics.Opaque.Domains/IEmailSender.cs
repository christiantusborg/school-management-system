namespace SharedLibrary.Basics.Opaque.Domains;

public interface IEmailSender
{
    /// <summary>Simple single-recipient HTML email (MFA codes, verification links).</summary>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rich email with CC/BCC, attachments and an optional sender override.
    /// Used by the letter-email feature to deliver the offer/admission PDF.
    /// </summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record EmailAttachment(string FileName, string MimeType, byte[] Content);

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    IReadOnlyList<string>? Cc = null,
    IReadOnlyList<string>? Bcc = null,
    IReadOnlyList<EmailAttachment>? Attachments = null,
    string? FromEmail = null,
    string? FromName = null);

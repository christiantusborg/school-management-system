using MimeKit;
using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

/// <summary>
/// Builds a MimeKit <see cref="MimeMessage"/> from an <see cref="EmailMessage"/>.
/// Shared by every transport (Brevo SMTP, Gmail API) so CC/BCC/attachment
/// handling stays identical regardless of how the bytes leave the building.
/// </summary>
internal static class EmailMimeBuilder
{
    public static MimeMessage Build(EmailMessage msg, string defaultFromEmail, string defaultFromName)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            msg.FromName ?? defaultFromName,
            msg.FromEmail ?? defaultFromEmail));
        message.To.Add(MailboxAddress.Parse(msg.To));

        foreach (var cc in msg.Cc ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(cc)) message.Cc.Add(MailboxAddress.Parse(cc.Trim()));
        foreach (var bcc in msg.Bcc ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(bcc)) message.Bcc.Add(MailboxAddress.Parse(bcc.Trim()));

        message.Subject = msg.Subject;

        var body = new BodyBuilder { HtmlBody = msg.HtmlBody };
        foreach (var att in msg.Attachments ?? Array.Empty<EmailAttachment>())
            body.Attachments.Add(att.FileName, att.Content, ContentType.Parse(att.MimeType));
        message.Body = body.ToMessageBody();

        return message;
    }
}

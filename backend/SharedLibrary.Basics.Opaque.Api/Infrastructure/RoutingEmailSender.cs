using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

/// <summary>
/// The app's <see cref="IEmailSender"/>. System mail (MFA codes, verification
/// links — the 3-arg overload) always goes through the built-in Brevo SMTP
/// transport. Rich letter emails (the <see cref="EmailMessage"/> overload)
/// route to Gmail when the admin has configured it in System Config → Email,
/// otherwise fall back to Brevo. Settings are read per-send so the admin can
/// switch transports from the portal without a redeploy.
/// </summary>
internal sealed class RoutingEmailSender(
    BrevoEmailSender brevo,
    OdinDbContext db) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        => brevo.SendAsync(to, subject, body, cancellationToken);

    public async Task SendAsync(EmailMessage msg, CancellationToken cancellationToken = default)
    {
        var settings = await db.MailSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        var fromEmail = msg.FromEmail ?? settings?.FromEmail;
        var fromName  = msg.FromName  ?? settings?.FromName;

        var useGmail = settings is { Provider: "Gmail" }
            && !string.IsNullOrWhiteSpace(settings.GmailServiceAccountJson)
            && !string.IsNullOrWhiteSpace(settings.GmailImpersonatedUser);

        if (useGmail)
        {
            // Gmail forces the From to be the impersonated mailbox (or a
            // verified alias); fall back to it when no explicit From is set.
            var gmailFrom = fromEmail ?? settings!.GmailImpersonatedUser!;
            var outgoing = msg with { FromEmail = gmailFrom, FromName = fromName ?? settings!.FromName };
            await GmailEmailDispatcher.SendAsync(
                settings!.GmailServiceAccountJson!, settings.GmailImpersonatedUser!,
                outgoing, gmailFrom, fromName ?? "Admission Team", cancellationToken);
            return;
        }

        var useSmtp = settings is { Provider: "Smtp" } && !string.IsNullOrWhiteSpace(settings.SmtpHost);
        if (useSmtp)
        {
            var smtpFrom = fromEmail ?? settings!.SmtpUsername ?? string.Empty;
            var outgoing = msg with { FromEmail = smtpFrom, FromName = fromName ?? settings!.FromName };
            await SmtpEmailDispatcher.SendAsync(
                settings!.SmtpHost!, settings.SmtpPort ?? 587, settings.SmtpUsername, settings.SmtpPassword, settings.SmtpSecurity,
                outgoing, smtpFrom, fromName ?? settings.FromName ?? "Admission Team", cancellationToken);
            return;
        }

        var brevoOutgoing = (fromEmail is not null || fromName is not null)
            ? msg with { FromEmail = fromEmail, FromName = fromName }
            : msg;
        await brevo.SendAsync(brevoOutgoing, cancellationToken);
    }
}

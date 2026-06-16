using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

internal sealed class BrevoEmailSender(IConfiguration configuration) : IEmailSender
{
    private readonly string _host      = configuration["Brevo:SmtpHost"]     ?? "smtp-relay.brevo.com";
    private readonly int    _port      = int.Parse(configuration["Brevo:SmtpPort"] ?? "587");
    private readonly string _login     = configuration["Brevo:SmtpLogin"]    ?? throw new InvalidOperationException("Brevo:SmtpLogin is required.");
    private readonly string _password  = configuration["Brevo:SmtpPassword"] ?? throw new InvalidOperationException("Brevo:SmtpPassword is required.");
    private readonly string _fromEmail = configuration["Brevo:FromEmail"]    ?? "noreply@example.com";
    private readonly string _fromName  = configuration["Brevo:FromName"]     ?? "Odin";

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        => SendAsync(new EmailMessage(to, subject, body), cancellationToken);

    public async Task SendAsync(EmailMessage msg, CancellationToken cancellationToken = default)
    {
        var message = EmailMimeBuilder.Build(msg, _fromEmail, _fromName);

        using var client = new SmtpClient();
        await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, cancellationToken).ConfigureAwait(false);
        await client.AuthenticateAsync(_login, _password, cancellationToken).ConfigureAwait(false);
        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }
}

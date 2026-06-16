using MailKit.Net.Smtp;
using MailKit.Security;
using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

/// <summary>
/// Sends mail through an arbitrary SMTP server (e.g. SiteGround) configured by
/// the admin in System Config → Email. Builds the MIME with the shared
/// <see cref="EmailMimeBuilder"/> so CC/BCC/attachments match every transport.
/// </summary>
internal static class SmtpEmailDispatcher
{
    public static async Task SendAsync(
        string host, int port, string? username, string? password, string? security,
        EmailMessage msg, string defaultFromEmail, string defaultFromName,
        CancellationToken ct)
    {
        var message = EmailMimeBuilder.Build(msg, defaultFromEmail, defaultFromName);

        var socketOptions = security switch
        {
            "StartTls"     => SecureSocketOptions.StartTls,
            "SslOnConnect" => SecureSocketOptions.SslOnConnect,
            "None"         => SecureSocketOptions.None,
            _              => SecureSocketOptions.Auto,
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, socketOptions, ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(username))
            await client.AuthenticateAsync(username, password ?? string.Empty, ct).ConfigureAwait(false);
        await client.SendAsync(message, ct).ConfigureAwait(false);
        await client.DisconnectAsync(true, ct).ConfigureAwait(false);
    }
}

namespace SharedLibrary.Basics.Opaque.Domains;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

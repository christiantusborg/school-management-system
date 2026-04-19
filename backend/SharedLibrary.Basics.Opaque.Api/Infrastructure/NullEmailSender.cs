using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

internal sealed class NullEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

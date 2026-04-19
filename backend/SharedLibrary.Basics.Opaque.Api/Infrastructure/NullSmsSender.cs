using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

internal sealed class NullSmsSender : ISmsSender
{
    public Task SendAsync(string to, string body, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

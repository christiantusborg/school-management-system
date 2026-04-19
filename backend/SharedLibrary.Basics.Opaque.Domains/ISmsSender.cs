namespace SharedLibrary.Basics.Opaque.Domains;

public interface ISmsSender
{
    Task SendAsync(string to, string body, CancellationToken cancellationToken = default);
}

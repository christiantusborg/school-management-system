namespace Odin.Api.Base.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string relativePath, CancellationToken ct);
    Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct);
    Task DeleteAsync(string storedPath, CancellationToken ct);
}

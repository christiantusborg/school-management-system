using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Odin.Api.Base.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration config, IHostEnvironment env)
    {
        var configured = config["Storage:LocalRoot"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "uploads")
            : Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(env.ContentRootPath, configured);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string relativePath, CancellationToken ct)
    {
        var safeRelative = Sanitise(relativePath);
        var fullPath = Path.Combine(_root, safeRelative);
        if (!fullPath.StartsWith(_root, StringComparison.Ordinal))
            throw new InvalidOperationException("Path traversal detected.");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct).ConfigureAwait(false);
        return safeRelative.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct)
    {
        var fullPath = ResolveFullPath(storedPath);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedPath, CancellationToken ct)
    {
        var fullPath = ResolveFullPath(storedPath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private string ResolveFullPath(string storedPath)
    {
        var safe = Sanitise(storedPath);
        var full = Path.Combine(_root, safe);
        if (!full.StartsWith(_root, StringComparison.Ordinal))
            throw new InvalidOperationException("Path traversal detected.");
        return full;
    }

    private static string Sanitise(string relativePath)
    {
        var normalised = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalised.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Relative path may not contain '..'.");
        return normalised.Replace('/', Path.DirectorySeparatorChar);
    }
}

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Odin.Api.Base.Cache;

public class MemoryTransientStateCache(IMemoryCache cache) : ITransientStateCache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(key, out string? json) && json is not null)
            return Task.FromResult(JsonSerializer.Deserialize<T>(json));
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        cache.Set(key, json, expiry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}

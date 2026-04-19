using Microsoft.Extensions.Caching.Memory;

namespace SharedLibrary.Basics.TransientStateCache;

public class MemoryTransientStateCache : ITransientStateCache
{
    private readonly IMemoryCache _cache;

    public MemoryTransientStateCache(IMemoryCache cache) => _cache = cache;

    public Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        _cache.Set(key, value, expiry);
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace SharedLibrary.Basics.TransientStateCache;

public class DistributedTransientStateCache : ITransientStateCache
{
    private readonly IDistributedCache _cache;

    public DistributedTransientStateCache(IDistributedCache cache) => _cache = cache;

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        var json = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        };
        await _cache.SetStringAsync(key, json, options);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _cache.GetStringAsync(key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}

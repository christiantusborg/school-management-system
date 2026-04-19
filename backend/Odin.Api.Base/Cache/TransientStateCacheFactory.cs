using Microsoft.Extensions.Configuration;

namespace Odin.Api.Base.Cache;

public class TransientStateCacheFactory(
    MemoryTransientStateCache memoryCache,
    DistributedTransientStateCache distributedCache,
    IConfiguration config) : ITransientStateCacheFactory
{
    public ITransientStateCache GetCache()
    {
        var provider = config["Cache:Provider"];
        return provider?.Equals("Distributed", StringComparison.OrdinalIgnoreCase) == true
            ? distributedCache
            : memoryCache;
    }
}

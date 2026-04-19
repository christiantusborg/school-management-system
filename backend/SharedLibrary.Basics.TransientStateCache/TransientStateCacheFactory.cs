using Microsoft.Extensions.Configuration;

namespace SharedLibrary.Basics.TransientStateCache;

public class TransientStateCacheFactory : ITransientStateCacheFactory
{
    private readonly MemoryTransientStateCache _memory;
    private readonly DistributedTransientStateCache _distributed;
    private readonly string _providerName;

    public TransientStateCacheFactory(
        MemoryTransientStateCache memory,
        DistributedTransientStateCache distributed,
        IConfiguration config)
    {
        _memory = memory;
        _distributed = distributed;
        _providerName = config.GetValue<string>("Cache:Provider") ?? "Memory";
    }

    public ITransientStateCache GetCache() =>
        _providerName.Equals("Distributed", StringComparison.OrdinalIgnoreCase)
            ? _distributed
            : _memory;
}

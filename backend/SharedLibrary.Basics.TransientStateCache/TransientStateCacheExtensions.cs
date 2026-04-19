using Microsoft.Extensions.DependencyInjection;

namespace SharedLibrary.Basics.TransientStateCache;

public static class TransientStateCacheExtensions
{
    /// <summary>
    /// Registers TransientStateCache services.
    /// Set Cache:Provider to "Distributed" in configuration to use IDistributedCache instead of IMemoryCache.
    /// When using Distributed, register your preferred provider before calling this method, e.g.:
    ///   builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
    /// The default AddDistributedMemoryCache is an in-process fallback — not suitable for multi-server.
    /// </summary>
    public static IServiceCollection AddTransientStateCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddScoped<MemoryTransientStateCache>();
        services.AddScoped<DistributedTransientStateCache>();
        services.AddScoped<ITransientStateCacheFactory, TransientStateCacheFactory>();
        services.AddScoped<ITransientStateCache>(sp =>
            sp.GetRequiredService<ITransientStateCacheFactory>().GetCache());
        return services;
    }
}

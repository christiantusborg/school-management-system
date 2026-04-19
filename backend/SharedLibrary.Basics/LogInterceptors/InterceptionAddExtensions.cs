using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace QuVian.SharedLibrary.Basics.LogInterceptors;

/// <summary>
/// Class InterceptionExtensions.
/// </summary>
public static partial class InterceptionExtensions
{
    /// <summary>
    /// Adds the intercepted singleton.
    /// </summary>
    /// <typeparam name="TInterface">The type of the interface to be intercepted.</typeparam>
    /// <typeparam name="TImplementation">The type of the concrete implementation of the interface.</typeparam>
    /// <typeparam name="TInterceptor">The type of the interceptor to use.</typeparam>
    /// <param name="services">The service collection to which the dependencies will be added.</param>
    public static void AddInterceptedSingleton<TInterface, TImplementation, TInterceptor>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
        where TInterceptor : class, IInterceptor
    {
        services.TryAddSingleton<IProxyGenerator, ProxyGenerator>();
        services.AddSingleton<TImplementation>();
        services.TryAddTransient<TInterceptor>();
        services.AddSingleton(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
            var impl = provider.GetRequiredService<TImplementation>();
            var interceptor = provider.GetRequiredService<TInterceptor>();
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(impl, interceptor);
        });
    }

    /// <summary>
    /// Adds the intercepted singleton.
    /// </summary>
    /// <typeparam name="TInterface">The type of the interface to be intercepted.</typeparam>
    /// <typeparam name="TInterceptor">The type of the interceptor used to intercept the implementation.</typeparam>
    /// <param name="services">The collection of service descriptors.</param>
    /// <param name="implementation">The specific implementation of the interface.</param>
    public static void AddInterceptedSingleton<TInterface, TInterceptor>(
        this IServiceCollection services, TInterface implementation)
        where TInterface : class
        where TInterceptor : class, IInterceptor
    {
        services.TryAddSingleton<IProxyGenerator, ProxyGenerator>();
        services.TryAddTransient<TInterceptor>();
        services.AddSingleton(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
            var interceptor = provider.GetRequiredService<TInterceptor>();
            return proxyGenerator.CreateInterfaceProxyWithTarget(implementation, interceptor);
        });
    }

    /// <summary>
    /// Adds the intercepted transient service.
    /// </summary>
    /// <typeparam name="TInterface">The type of the interface.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
    /// <typeparam name="TInterceptor">The type of the interceptor.</typeparam>
    /// <param name="services">The service collection.</param>
    public static void AddInterceptedTransient<TInterface, TImplementation, TInterceptor>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
        where TInterceptor : class, IInterceptor
    {
        services.TryAddSingleton<IProxyGenerator, ProxyGenerator>();
        services.AddTransient<TImplementation>();
        services.TryAddTransient<TInterceptor>();
        services.AddTransient(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
            var impl = provider.GetRequiredService<TImplementation>();
            var interceptor = provider.GetRequiredService<TInterceptor>();
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(impl, interceptor);
        });
    }
}


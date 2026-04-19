using Microsoft.Extensions.DependencyInjection;

namespace QuVian.SharedLibrary.Basics.LogInterceptors;

/// <summary>
/// Class InterceptionExtensions.
/// </summary>
public static partial class InterceptionExtensions
{
    /// <summary>
    /// Attaches the intercepted singleton.
    /// </summary>
    /// <param name="services">The service collection to which intercepted singletons will be attached.</param>
    public static void AttachInterceptedSingleton(this IServiceCollection services)
    {
        var logInterceptorAttachType = typeof(IInterceptorAttach);
        var providers = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .Where(x => logInterceptorAttachType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
            .Select(Activator.CreateInstance)
            .Cast<IInterceptorAttach>().ToList();

        foreach (var logInterceptorAttach in providers)
        {
            services = logInterceptorAttach.Attach(services);
        }
    }
}


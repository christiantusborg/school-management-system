using Microsoft.Extensions.DependencyInjection;

namespace QuVian.SharedLibrary.Basics.LogInterceptors;

/// <summary>
/// Interface IInterceptorAttach.
/// </summary>
public interface IInterceptorAttach
{
    /// <summary>
    /// Attaches the specified services.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>IServiceCollection.</returns>
    IServiceCollection Attach(IServiceCollection services);
}


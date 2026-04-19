using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Endpoints.Attributes.Deprecations;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds V1 adjuncts to the service collection for endpoint configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddEndpointV1Deprecations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEndpointSummaryAppend, EndpointV1SummaryDeprecated>();
        services.AddSingleton<IEndpointDescriptionAppend, EndpointV1DescriptionDeprecated>();

        return services;
    }
}
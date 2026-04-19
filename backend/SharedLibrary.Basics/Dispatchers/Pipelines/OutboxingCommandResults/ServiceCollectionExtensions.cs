namespace QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.OutboxingCommandResults;

/// <summary>
///     Class ServiceCollectionExtensions.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds MediatR Permission pipeline behaviour.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>IServiceCollection.</returns>
    public static IServiceCollection AddPipelineBehavioursOutboxingCommandResults<TAsymmetricOutboxingCommandResultsEncryptionService>(this IServiceCollection services)
    {

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(OutboxingCommandResultPipelineBehaviour<,>));
        return services;
    }
}
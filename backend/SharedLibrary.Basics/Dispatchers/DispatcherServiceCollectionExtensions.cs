using Microsoft.Extensions.DependencyInjection.Extensions;
using QuVian.SharedLibrary.Basics.SuccessOrFailures;

namespace QuVian.SharedLibrary.Basics.Dispatchers;

/// <summary>
/// Provides extension methods for registering dispatcher services in an <see cref="IServiceCollection"/>.
/// </summary>
[SuppressMessage("ReSharper", "UnusedVariable")]
[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public static class DispatcherServiceCollectionExtensions
{
    /// <summary>
    /// Adds dispatcher services to the specified <see cref="IServiceCollection"/> by scanning the provided assemblies
    /// for implementations of <see cref="IHandleableCommand{TRequest, TValidator, THandler, TResult}"/> and
    /// registering the associated request handlers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the dispatcher services will be added.</param>
    /// <param name="assemblies">An array of assemblies to scan for dispatcher command types and handlers.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>, allowing for chaining of further service registrations.</returns>
    public static IServiceCollection AddDispatcher(this IServiceCollection services, Assembly[] assemblies)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[Dispatcher] Scanning assemblies for IHandleableCommand<> registrations...");
        Console.ResetColor();

        var commandTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleableCommand<,,,>)))
            .ToList();

        foreach (var commandType in commandTypes)
        {
            try
            {
                var handleableInterface = commandType
                    .GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleableCommand<,,,>));

                var args = handleableInterface.GetGenericArguments();

                var requestType = args[0];    // TRequest (command)
                var handlerType = args[2];    // THandler
                var resultType = args[3];     // TResult

                var requestHandlerInterface = typeof(IRequestHandler<,>)
                    .MakeGenericType(commandType, typeof(SuccessOrFailure<>).MakeGenericType(resultType));

                services.TryAddScoped(requestHandlerInterface, handlerType);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[Dispatcher] Registered: {requestHandlerInterface.FullName} -> {handlerType.FullName}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Dispatcher] ⚠️ Skipped type {commandType.FullName}: {ex.Message}");
                Console.ResetColor();
            }
        }

        services.AddSingleton<Dispatcher>();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[Dispatcher] Dispatcher registered successfully.\n");
        Console.ResetColor();

        return services;
    }
}
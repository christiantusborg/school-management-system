using QuVian.SharedLibrary.Basics.SuccessOrFailures;


namespace QuVian.SharedLibrary.Basics.Dispatchers;

/// <summary>
/// Acts as a mediator for dispatching commands or messages in a structured application.
/// The `Dispatcher` class enables coordination by delegating requests to their appropriate
/// handlers and supports extensibility via a pipeline of behaviors.
/// </summary>
public sealed class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<object>>>
        _handlerDelegates = new();

    private static readonly ConcurrentDictionary<Type, MethodInfo> _behaviorMethods = new();

    /// <summary>
    /// Asynchronously sends a handleable command or message to its corresponding handler
    /// and processes the associated behaviors and pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result, which must implement <see cref="IMessageQueue"/>.</typeparam>
    /// <param name="command">The command or message to handle, implementing <see cref="IHandleable{TResult}"/>.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the operation to complete.
    /// Defaults to the default cancellation token value if not provided.
    /// </param>
    /// <returns>
    /// An asynchronous task that resolves to an <see cref="SuccessOrFailure{TSuccess}"/> result,
    /// wrapping the output from the corresponding handler.
    /// </returns>
    public async Task<SuccessOrFailure<TResult>> SendAsync<TResult>(
        IHandleable<TResult> command,
        CancellationToken cancellationToken = default)
        where TResult : IMessageQueue
    {
        var commandType = command.GetType();
        var resultType = typeof(SuccessOrFailure<TResult>);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(commandType, resultType);
        var handler = serviceProvider.GetRequiredService(handlerType);

        var handlerDelegate = _handlerDelegates.GetOrAdd(handlerType, ht =>
        {
            var method = ht.GetMethod("HandleAsync")!;
            return (h, request, token) =>
            {
                var task = (Task)method.Invoke(h, [request, token])!;
                return task.ContinueWith(t =>
                {
                    var resultProperty = t.GetType().GetProperty("Result")!;
                    return resultProperty.GetValue(t)!;
                }, token);
            };
        });

        async Task<SuccessOrFailure<TResult>> FinalHandler()
        {
            var result = await handlerDelegate(handler, command, cancellationToken);
            return (SuccessOrFailure<TResult>)result;
        }

        // Build pipeline
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(commandType, resultType);
        var behaviors = serviceProvider.GetServices(behaviorType).ToList();

        var pipeline = FinalHandler;

        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            if (behavior is null) continue;

            var method = _behaviorMethods.GetOrAdd(behavior.GetType(), bt =>
            {

                var handleMethod = bt.GetMethod("HandleAsync");
                if (handleMethod is null)
                    throw new InvalidOperationException($"Behavior '{bt.Name}' does not contain a 'Handle' method.");
                return handleMethod;
            });


            var behaviorInstance = behavior;
            var next = pipeline;

            pipeline = () =>
            {
                var result = (Task)method.Invoke(behaviorInstance, [command, cancellationToken, next])!;
                return result.ContinueWith(t =>
                {
                    var resultProperty = t.GetType().GetProperty("Result")!;
                    return (SuccessOrFailure<TResult>)resultProperty.GetValue(t)!;
                }, cancellationToken);
            };
        }

        return await pipeline();
    }
}
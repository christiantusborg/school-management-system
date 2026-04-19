using QuVian.SharedLibrary.Basics.SuccessOrFailures;


namespace QuVian.SharedLibrary.Basics.Dispatchers;

public interface IDispatcher
{
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
    Task<SuccessOrFailure<TResult>> SendAsync<TResult>(
        IHandleable<TResult> command,
        CancellationToken cancellationToken = default)
        where TResult : IMessageQueue;
}
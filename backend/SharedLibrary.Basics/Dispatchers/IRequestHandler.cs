namespace QuVian.SharedLibrary.Basics.Dispatchers;

/// <summary>
/// Represents a handler for processing requests asynchronously and producing a result.
/// </summary>
/// <typeparam name="TRequest">The type of the request to handle.</typeparam>
/// <typeparam name="TResult">The type of the result produced after handling the request.</typeparam>
public interface IRequestHandler<TRequest, TResult>
{
    /// <summary>
    /// Asynchronously handles a request and returns the corresponding result.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the request handling.</returns>
    Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
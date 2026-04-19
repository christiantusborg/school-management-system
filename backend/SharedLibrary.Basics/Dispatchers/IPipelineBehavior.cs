namespace QuVian.SharedLibrary.Basics.Dispatchers;

/// <summary>
/// Defines a pipeline behavior that allows the application of cross-cutting concerns to the handling process of a request.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the request handler.</typeparam>
public interface IPipelineBehavior<TRequest, TResult>
{
    /// <summary>
    /// Handles the specified request by applying a behavior in the processing pipeline.
    /// </summary>
    /// <param name="request">The request instance to be handled.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete to support cancellation.</param>
    /// <param name="next">A delegate to call the next behavior or handler in the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation and contains the result of the handled request.</returns>
    Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<Task<TResult>> next);
}
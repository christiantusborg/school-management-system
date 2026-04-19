using QuVian.SharedLibrary.Basics.SuccessOrFailures;


namespace QuVian.SharedLibrary.Basics.Dispatchers;

/// <summary>
/// Represents a handleable request that produces a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">
/// The type of the result expected from handling this request.
/// Must implement the <see cref="IMessageQueue"/> interface.
/// </typeparam>
public interface IHandleable<TResult> : IRequest<SuccessOrFailure<TResult>>
    where TResult : IMessageQueue;
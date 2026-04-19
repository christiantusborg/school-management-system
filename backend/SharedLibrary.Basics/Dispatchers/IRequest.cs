namespace QuVian.SharedLibrary.Basics.Dispatchers;

/// <summary>
/// Represents a request that is expected to produce a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">
/// The type of the result returned when this request is handled.
/// </typeparam>
public interface IRequest<TResult> { }
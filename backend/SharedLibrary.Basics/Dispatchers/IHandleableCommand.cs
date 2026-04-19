using QuVian.SharedLibrary.Basics.SuccessOrFailures;

using QuVian.SharedLibrary.Validations;

namespace QuVian.SharedLibrary.Basics.Dispatchers;

/// <summary>
/// Represents an interface for a handleable command that can be dispatched and processed using a request handler,
/// a validator, and provides a specific result type.
/// </summary>
/// <typeparam name="TRequest">
/// The type of the command implementing this interface.
/// It must adhere to the constraints of being a handleable command.
/// </typeparam>
/// <typeparam name="TValidator">
/// The type of the validator responsible for validating the command request.
/// Must inherit from <see cref="BaseValidator{T}"/>.
/// </typeparam>
/// <typeparam name="THandler">
/// The type of the handler responsible for processing the command request.
/// Must implement <see cref="IRequestHandler{TRequest, TResult}"/>.
/// </typeparam>
/// <typeparam name="TResult">
/// The type of the result produced by the handler upon successfully processing the command.
/// Must implement <see cref="IMessageQueue"/>.
/// </typeparam>
public interface IHandleableCommand<TRequest, TValidator, THandler, TResult> : IHandleable<TResult>
    where TRequest : IHandleableCommand<TRequest, TValidator, THandler, TResult>
    where TResult : IMessageQueue
    where TValidator : BaseValidator<TRequest>
    where THandler : IRequestHandler<TRequest, SuccessOrFailure<TResult>>;
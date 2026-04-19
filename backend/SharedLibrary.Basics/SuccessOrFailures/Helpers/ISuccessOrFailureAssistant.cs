using QuVian.SharedLibrary.Basics.SuccessOrFailures.Exceptions;

namespace QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;

/// <summary>
/// Provides functionality for handling operations that can result in successOrFailure success or failure outcomes.
/// </summary>
/// <remarks>
/// This interface offers a set of methods for evaluating and managing responses and exceptions tied to
/// operations with outcomes that encapsulate both successful and unsuccessful scenarios.
/// Implementations can determine response types, construct appropriate responses, and create consistent exceptions.
/// </remarks>
public interface ISuccessOrFailureAssistant
{
    /// <summary>
    /// Determines whether the specified type is a generic type of SuccessOrFailure.
    /// </summary>
    /// <typeparam name="TResponse">The type to evaluate.</typeparam>
    /// <returns>True if the type is a generic type of SuccessOrFailure; otherwise, false.</returns>
    bool IsSuccessOrFailure<TResponse>();

    /// <summary>
    /// Creates a response of the specified type using the provided exception details.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response to create.</typeparam>
    /// <param name="ext">The exception containing metadata and details for generating the response.</param>
    /// <returns>An instance of the specified response type containing the relevant details from the exception.</returns>
    TResponse CreateResponse<TResponse>(SuccessOrFailureException ext);

    /// <summary>
    /// Creates a SuccessOrFailureException with additional metadata to represent the result of an operation.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request associated with the exception.</typeparam>
    /// <param name="request">The request that contains the data for the operation.</param>
    /// <param name="origin">The origin or source of the exception.</param>
    /// <param name="message">A descriptive message detailing the exception.</param>
    /// <param name="httpStatusCodes">The HTTP status code correlated with the exception.</param>
    /// <param name="assistantMessage">Additional context or message for the assistant (default is "AssistantMessage").</param>
    /// <returns>An instance of <see cref="SuccessOrFailureException"/> containing the exception details.</returns>
    SuccessOrFailureException CreateSuccessOrFailureException<TRequest>(TRequest request, string origin, string message,
        int httpStatusCodes, string assistantMessage);
}
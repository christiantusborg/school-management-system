using QuVian.SharedLibrary.Basics.SuccessOrFailures.Exceptions;

namespace QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;

/// <summary>
/// Provides utility methods for managing operations and exceptions pertaining to types
/// representing 'Success or Failure' scenarios. This class facilitates type checking,
/// response creation, and exception handling to streamline success or failure workflows.
/// </summary>
public class SuccessOrFailureAssistant : ISuccessOrFailureAssistant
{
    /// <summary>
    /// Determines whether the specified type is a generic type of <see cref="SuccessOrFailure{TSuccess}"/>.
    /// </summary>
    /// <typeparam name="TResponse">The type to check.</typeparam>
    /// <returns><c>true</c> if the specified type is a generic type of <see cref="SuccessOrFailure{TSuccess}"/>; otherwise, <c>false</c>.</returns>
    public bool IsSuccessOrFailure<TResponse>()
    {
        var responseType = typeof(TResponse);
        return responseType.GetGenericTypeDefinition() == typeof(SuccessOrFailure<>);
    }

    /// <summary>
    /// Creates a response of the specified type using the provided exception.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response to create.</typeparam>
    /// <param name="ext">The exception containing information to construct the response.</param>
    /// <returns>An instance of <typeparamref name="TResponse"/> initialized using the provided exception.</returns>
    public TResponse CreateResponse<TResponse>(SuccessOrFailureException ext)
    {
        Type? responseType = typeof(TResponse);
        var response = (TResponse)Activator.CreateInstance(responseType, ext)!;
        return response;
    }

    /// <summary>
    /// Creates a new instance of <see cref="SuccessOrFailureException"/> with the specified parameters.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object associated with the exception.</typeparam>
    /// <param name="request">The request object containing command values.</param>
    /// <param name="origin">The origin or source of the exception.</param>
    /// <param name="message">The detailed message of the exception.</param>
    /// <param name="httpStatusCodes">The HTTP status codes associated with the exception.</param>
    /// <param name="assistantMessage">An optional assistant message providing additional information. Default is "AssistantMessage".</param>
    /// <returns>An instance of <see cref="SuccessOrFailureException"/> containing the provided details.</returns>
    public SuccessOrFailureException CreateSuccessOrFailureException<TRequest>(TRequest request, string origin, string message,
        int httpStatusCodes, string assistantMessage = "AssistantMessage")
    {
        var successOrFailureAssistantMessage = new SuccessOrFailureAssistantMessage
        {
            HttpStatusCode = httpStatusCodes,
            Message = message,
            Origin = origin,
            Command = typeof(TRequest).Name,
            CommandValues = request,
        };

        var ext = new SuccessOrFailureException(successOrFailureAssistantMessage, assistantMessage, httpStatusCodes);
        return ext;
    }
}
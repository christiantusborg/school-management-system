using Microsoft.AspNetCore.Http;

namespace QuVian.SharedLibrary.Basics.SuccessOrFailures.Exceptions;

/// <summary>
/// Represents an exception that encapsulates additional metadata for handling successful or failed operations.
/// </summary>
/// <remarks>
/// The <see cref="SuccessOrFailureException"/> class is used to provide structured information, including a custom result,
/// a descriptive message, and an HTTP status code, for scenarios where an operation succeeds or fails in a specific manner.
/// </remarks>
public sealed class SuccessOrFailureException : Exception
{
    /// <summary>
    /// Represents an exception that encapsulates structured information about a result,
    /// an optional message, and an HTTP status code.
    /// This exception is particularly useful for scenarios where a standardized
    /// error format along with a status code is required.
    /// </summary>
    /// <remarks>
    /// The exception contains additional data in the <c>Data</c> property, which includes:
    /// - "Result": The result object providing contextual information.
    /// - "Message": The accompanying error message.
    /// - "HttpStatusCode": The HTTP status code representing the nature of the error.
    /// </remarks>
    public SuccessOrFailureException(object? result, string? message, int statusCode = StatusCodes.Status400BadRequest)
        : base(message)
    {
        Data.Add("Result", result);
        Data.Add("Message", message);
        Data.Add("HttpStatusCode", statusCode);
    }
}
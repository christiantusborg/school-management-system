namespace QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;

/// <summary>
/// Encapsulates metadata and contextual details to facilitate the identification
/// and explanation of the outcome of an operation, providing clarity about
/// the operation’s command, origin, HTTP status code, and additional messages.
/// </summary>
public class SuccessOrFailureAssistantMessage
{
    /// <summary>
    /// Specifies the command related to the context of an operation or task.
    /// </summary>
    /// <remarks>
    /// This property indicates the type of operation being performed and is immutable after initialization.
    /// It is typically derived from the type or nature of the request being processed.
    /// </remarks>
    public string? Command { get; init; }

    /// <summary>
    /// Represents the values associated with the command being executed in the context of the assistant message.
    /// </summary>
    /// <remarks>
    /// This property is used to provide additional information or data relevant to the command.
    /// It is initialized during the creation of the assistant message and is read-only.
    /// </remarks>
    public object? CommandValues { get; init; }

    /// <summary>
    /// Represents the origin or source context associated with the operation result.
    /// </summary>
    /// <remarks>
    /// This property provides information about where the operation originated from,
    /// potentially useful for debugging or tracing the operational flow.
    /// This property is read-only and is initialized during object construction.
    /// </remarks>
    public string? Origin { get; init; }

    /// <summary>
    /// Represents the HTTP status code associated with the result of an operation.
    /// </summary>
    /// <remarks>
    /// This property provides an integer representation of the HTTP status code,
    /// indicating the outcome of an operation, such as success, client error, or server error.
    /// It is immutable and set during the initialization of the message object.
    /// </remarks>
    public int HttpStatusCode { get; init; }

    /// <summary>
    /// Represents the message that conveys the outcome details of the operation,
    /// including any additional contextual or descriptive information.
    /// </summary>
    /// <remarks>
    /// This property is used to store a human-readable explanation or description associated
    /// with the result, which can assist in understanding the success or failure context.
    /// It is initialized during the construction of the assistant message and is immutable.
    /// </remarks>
    public string? Message { get; init; }
}
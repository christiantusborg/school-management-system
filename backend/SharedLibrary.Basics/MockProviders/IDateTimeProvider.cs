using Microsoft.Extensions.Logging;
using QuVian.SharedLibrary.Basics.LogInterceptors;

namespace QuVian.SharedLibrary.Basics.MockProviders;

/// <summary>
/// Represents a provider for accessing the current UTC date and time,
/// allowing for abstraction and easier testing by enabling mocking of datetime values.
/// </summary>
[LogInterceptorDefaultLogLevel(LogLevel.Debug)]
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    /// <value>The current UTC date and time.</value>
    DateTime UtcNow { get; }
}


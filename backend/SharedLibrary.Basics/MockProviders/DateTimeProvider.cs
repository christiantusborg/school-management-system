using Microsoft.Extensions.Logging;
using QuVian.SharedLibrary.Basics.LogInterceptors;

namespace QuVian.SharedLibrary.Basics.MockProviders;

/// <summary>
/// Provides a mechanism to retrieve the current UTC date and time.
/// Primarily used to facilitate mocking for testing purposes.
/// </summary>
[LogInterceptorDefaultLogLevel(LogLevel.Debug)]
public class DateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Gets the current Coordinated Universal Time (UTC).
    /// </summary>
    /// <value>The current date and time in UTC format.</value>
    public DateTime UtcNow => DateTime.UtcNow;
}


using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace QuVian.SharedLibrary.Basics.LogInterceptors;

/// <summary>
/// Class LogInterceptorDefaultLogLevelAttribute.
/// Implements the <see cref="System.Attribute" />.
/// </summary>
/// <remarks>
/// This attribute specifies the default logging level for methods, types, or assemblies where it is applied.
/// It uses the <see cref="Microsoft.Extensions.Logging.LogLevel" /> enumeration to determine the log level.
/// </remarks>
/// <seealso cref="System.Attribute" />
[AttributeUsage(AttributeTargets.All)]
public sealed class LogInterceptorDefaultLogLevelAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogInterceptorDefaultLogLevelAttribute" /> class.
    /// </summary>
    /// <param name="logLevel">The log level.</param
    public LogInterceptorDefaultLogLevelAttribute([Required] LogLevel logLevel = LogLevel.Critical)
    {
        LogLevel = logLevel;
    }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    /// <value>The log level used for logging operations.</value>
    public LogLevel LogLevel { get; }
}


namespace QuVian.SharedLibrary.Basics.LogInterceptors;

/// <summary>
/// Class LogInterceptorExcludeAttribute.
/// Marks a parameter, property, or method to be excluded from log interception.
/// Implements the <see cref="System.Attribute" />.
/// </summary>
/// <remarks>
/// Used by the LogInterceptorDefault class to identify and handle members
/// that should not be included in the logging process.
/// </remarks>
/// <seealso cref="System.Attribute" />
[AttributeUsage(AttributeTargets.All)]
public sealed class LogInterceptorExcludeAttribute : Attribute
{
}


using System.Diagnostics.CodeAnalysis;

namespace QuVian.SharedLibrary.Endpoints.Attributes.Deprecations;

/// <summary>
///     Represents an attribute used to mark an API endpoint as deprecated.
/// </summary>
/// <remarks>
///     This attribute specifies the date when the endpoint was deprecated and the date when it will be considered
///     end-of-life (EOL).
///     After the end-of-life date, the endpoint may no longer function or be supported.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
[SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
public sealed class DeprecatedAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DeprecatedAttribute" /> class with the specified deprecation date and
    ///     end-of-life period.
    /// </summary>
    /// <param name="year">The year when the endpoint was deprecated.</param>
    /// <param name="month">The month when the endpoint was deprecated.</param>
    /// <param name="day">The day when the endpoint was deprecated.</param>
    /// <param name="DaysToEndOfLife">
    ///     The number of days after the deprecation date to mark the endpoint as end-of-life. The
    ///     default is 180 days.
    /// </param>
    public DeprecatedAttribute(int year, int month, int day, int DaysToEndOfLife = 180)
    {
        DeprecatedDate = new DateOnly(year, month, day);
        EndOfLifeDate = DeprecatedDate.AddDays(DaysToEndOfLife);
    }

    /// <summary>
    ///     Gets the date on which the endpoint was marked as deprecated.
    /// </summary>
    public DateOnly DeprecatedDate { get; internal set; }

    /// <summary>
    ///     Gets the end-of-life date of the endpoint, after which it may no longer be available or supported.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public DateOnly EndOfLifeDate { get; internal set; }
}


namespace QuVian.SharedLibrary.Basics.Filters;

/// <summary>
///     The operation.
/// </summary>
public enum Operation
{
    /// <summary>
    ///     The equals.
    /// </summary>
    Equals = 0,

    /// <summary>
    ///     The greater than.
    /// </summary>
    GreaterThan = 1,

    /// <summary>
    ///     The less than.
    /// </summary>
    LessThan = 2,

    /// <summary>
    ///     The greater than or equal.
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    ///     The less than or equal.
    /// </summary>
    LessThanOrEqual = 4,

    /// <summary>
    ///     The contains.
    /// </summary>
    Contains = 5,

    /// <summary>
    ///     The starts with.
    /// </summary>
    StartsWith = 6,

    /// <summary>
    ///     The ends with.
    /// </summary>
    EndsWith = 7,

    /// <summary>
    ///     The ends with.
    /// </summary>
    NotEqual = 8
}

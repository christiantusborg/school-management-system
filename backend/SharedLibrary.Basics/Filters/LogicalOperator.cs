namespace QuVian.SharedLibrary.Basics.Filters;

/// <summary>
/// Specifies how a filter condition is combined with the preceding condition.
/// </summary>
public enum LogicalOperator
{
    /// <summary>Both conditions must be true.</summary>
    And = 0,

    /// <summary>Either condition must be true.</summary>
    Or = 1
}

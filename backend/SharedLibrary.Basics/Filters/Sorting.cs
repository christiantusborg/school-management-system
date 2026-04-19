namespace QuVian.SharedLibrary.Basics.Filters;

/// <summary>
///     Represents a sorting configuration for a data source.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class Sorting
{
    /// <summary>
    ///     Gets or sets the name of the field to sort by.
    /// </summary>
    public string SortField { get; init; } = null!;

    /// <summary>
    ///     Gets or sets the direction of the sort.
    /// </summary>
    public SortDirection Direction { get; init; }
}

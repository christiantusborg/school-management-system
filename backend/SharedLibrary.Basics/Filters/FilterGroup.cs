namespace QuVian.SharedLibrary.Basics.Filters;

/// <summary>
/// A group of filters evaluated together as a unit, then combined with
/// surrounding groups via <see cref="GroupOperator"/>.
/// Fully serializable — can be sent from the frontend as JSON.
/// </summary>
/// <remarks>
/// Filters inside the group combine using each filter's own
/// <see cref="Filter.LogicalOperator"/>. The <see cref="GroupOperator"/>
/// controls how THIS group joins the expression built so far.
///
/// Example JSON from frontend:
/// <code>
/// [
///   {
///     "filters": [
///       { "propertyName": "FirstName", "operation": 5, "value": "Willy" },
///       { "propertyName": "LastName",  "operation": 5, "value": "Willy", "logicalOperator": 1 }
///     ],
///     "groupOperator": 0
///   },
///   {
///     "filters": [
///       { "propertyName": "IsActive", "operation": 0, "value": true }
///     ],
///     "groupOperator": 0
///   }
/// ]
/// </code>
/// </remarks>
public sealed class FilterGroup
{
    /// <summary>The filters evaluated inside this group.</summary>
    public IReadOnlyList<Filter> Filters { get; init; } = [];

    /// <summary>How this group is combined with the preceding group or flat filters.</summary>
    public LogicalOperator GroupOperator { get; init; } = LogicalOperator.And;
}

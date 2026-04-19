namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Represents a request that contains multiple items of a specified type.
/// </summary>
/// <typeparam name="T">The type of the items included in the bulk request.</typeparam>
public class BulkRequest<T>
{
    public required IList<T> Items { get; init; }
}
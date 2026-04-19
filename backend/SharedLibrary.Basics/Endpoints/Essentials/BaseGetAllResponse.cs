namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Class BaseGetAllResponse. This class cannot be inherited.
/// </summary>
/// <typeparam name="T"></typeparam>
public class BaseGetAllResponse<T>
{
    /// <summary>
    /// Represents the total count of items retrieved.
    /// </summary>
    public required int Total { get; init; }

    /// <summary>
    /// Collection of items.
    /// </summary>
    public required IList<T> Items { get; init; }
}
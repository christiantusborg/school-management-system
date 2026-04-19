namespace QuVian.SharedLibrary.Basics.Repositories.Interfaces;

/// <summary>
/// Represents an entity that includes a deletion timestamp.
/// </summary>
public interface IDeletedAtEntity : IEntity
{
    /// <summary>
    /// Gets or sets the timestamp indicating when the entity was deleted.
    /// </summary>
    /// <remarks>
    /// If the value is null, it indicates that the entity has not been deleted.
    /// </remarks>
    DateTime? DeletedAt { get; set; }
}
namespace QuVian.SharedLibrary.Basics.Repositories.Interfaces;

/// <summary>
/// Interface defining an entity with a creation timestamp.
/// </summary>
public interface ICreatedAtEntity : IEntity
{
    /// <summary>
    /// Gets or sets the timestamp indicating when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }
}
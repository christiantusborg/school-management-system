namespace QuVian.SharedLibrary.Basics.Repositories.Interfaces;

/// <summary>
/// Represents an entity that includes information about its last modification date and time.
/// </summary>
public interface IModifiedAtEntity : IEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was last modified.
    /// </summary>
    DateTime ModifiedAt { get; set; }
}
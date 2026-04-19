using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.OutboxingCommandResults.Models;

/// <summary>
/// Represents the result of an outboxing command.
/// </summary>
public sealed class OutboxingCommandResult : IEntity
{
    public required Guid OutboxingId { get; set; }
    /// <summary>
    /// Gets or sets the interface property.
    /// </summary>
    /// <value>
    /// A string representing the interface.
    /// </value>
    public required string Interface { get; set; }
    /// <summary>
    /// The result of the interface.
    /// </summary>
    /// <value>
    /// A string representing the result of the interface.
    /// </value>
    public required string InterfaceResult  { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the object was created.
    /// </summary>
    /// <value>
    /// The date and time when the object was created.
    /// </value>
    public DateTime CreatedAt { get; set; }
}
using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.AuditLogs.Models;

public class AuditLog  : IEntity
{
    public required Guid AuditLogId { get; init; }
    public required string Command { get; init; }
    public required string CommandData { get; init; }
    public required string ResultData { get; init; }
    public required Guid UserId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string TraceIdentifier  { get; init; }

}
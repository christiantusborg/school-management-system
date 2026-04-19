namespace QuVian.CaseApi.CaseFiles.V1.List.Command;

public sealed class CaseFilesV1ListCommandResult : ICaseFilesV1ListCommandResultQueue
{
    public required List<CaseFileItem> Items { get; init; }
}

public class CaseFileItem
{
    public required Guid CaseFileId { get; init; }
    public required string Name { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public int MinLevel { get; init; }
    public string AccessMode { get; init; } = null!;
    public required string CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}

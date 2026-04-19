namespace QuVian.CaseApi.CaseFiles.V1.Create.Command;

public sealed record CaseFilesV1CreateCommand : IHandleableCommand<
    CaseFilesV1CreateCommand,
    CaseFilesV1CreateCommandValidator,
    CaseFilesV1CreateCommandHandler,
    CaseFilesV1CreateCommandResult>
{
    public required Guid CaseId { get; init; }
    public required string Name { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public required string StoragePath { get; init; }
    public int MinLevel { get; init; }
    public CaseFileAccessMode AccessMode { get; init; } = CaseFileAccessMode.Hierarchical;
    public required string CreatedByUserId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;

    /// <summary>One CaseFileLevelKey per qualifying access level.</summary>
    public required List<FileLevelKeyCommand> LevelKeys { get; init; }
}

public record FileLevelKeyCommand
{
    public int Level { get; init; }
    public required byte[] KemCiphertext { get; init; }
    public required byte[] EncryptedFileKey { get; init; }
    public required byte[] Nonce { get; init; }
}

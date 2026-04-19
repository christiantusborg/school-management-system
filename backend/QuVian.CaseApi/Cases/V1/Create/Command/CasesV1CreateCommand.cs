namespace QuVian.CaseApi.Cases.V1.Create.Command;

public sealed record CasesV1CreateCommand : IHandleableCommand<
    CasesV1CreateCommand,
    CasesV1CreateCommandValidator,
    CasesV1CreateCommandHandler,
    CasesV1CreateCommandResult>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public CasePriority Priority { get; init; } = CasePriority.Medium;
    public DateTime? DueDate { get; init; }
    public required string CreatedByUserId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;

    /// <summary>6 level key pairs — public key + creator's wrapped private key per level.</summary>
    public required List<CaseLevelKeyCommand> LevelKeyPairs { get; init; }
}

public record CaseLevelKeyCommand
{
    public int Level { get; init; }
    public required byte[] PublicKey { get; init; }
    public required byte[] KemCiphertext { get; init; }
    public required byte[] EncryptedLevelPrivKey { get; init; }
    public required byte[] Nonce { get; init; }
}

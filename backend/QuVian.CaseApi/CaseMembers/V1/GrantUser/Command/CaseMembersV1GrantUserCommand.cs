namespace QuVian.CaseApi.CaseMembers.V1.GrantUser.Command;

public sealed record CaseMembersV1GrantUserCommand : IHandleableCommand<
    CaseMembersV1GrantUserCommand,
    CaseMembersV1GrantUserCommandValidator,
    CaseMembersV1GrantUserCommandHandler,
    CaseMembersV1GrantUserCommandResult>
{
    public required Guid CaseId { get; init; }
    public required string TargetUserId { get; init; }
    public required string ActorUserId { get; init; }
    public int Level { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;

    /// <summary>
    /// Wrapped private keys for levels Level through 6 — one per level.
    /// Each entry contains the level private key encrypted to the target user's personal ML-KEM public key.
    /// </summary>
    public required List<WrappedLevelKey> WrappedKeys { get; init; }
}

public record WrappedLevelKey
{
    public int Level { get; init; }
    public required byte[] KemCiphertext { get; init; }
    public required byte[] EncryptedLevelPrivKey { get; init; }
    public required byte[] Nonce { get; init; }
}

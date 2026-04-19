namespace QuVian.CaseApi.CaseMembers.V1.GrantTeam.Command;

public sealed record CaseMembersV1GrantTeamCommand : IHandleableCommand<
    CaseMembersV1GrantTeamCommand,
    CaseMembersV1GrantTeamCommandValidator,
    CaseMembersV1GrantTeamCommandHandler,
    CaseMembersV1GrantTeamCommandResult>
{
    public required Guid CaseId { get; init; }
    public required Guid TeamId { get; init; }
    public required string ActorUserId { get; init; }
    public int Level { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;

    /// <summary>
    /// Level private keys for levels Level through 6, each encrypted with the team's AES-256 symmetric key.
    /// </summary>
    public required List<TeamWrappedLevelKey> TeamWrappedKeys { get; init; }
}

public record TeamWrappedLevelKey
{
    public int Level { get; init; }
    public required byte[] EncryptedLevelPrivKey { get; init; }
    public required byte[] Nonce { get; init; }
}

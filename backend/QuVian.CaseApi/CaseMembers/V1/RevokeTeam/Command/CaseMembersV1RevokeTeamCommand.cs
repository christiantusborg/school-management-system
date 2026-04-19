namespace QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Command;

public sealed record CaseMembersV1RevokeTeamCommand : IHandleableCommand<
    CaseMembersV1RevokeTeamCommand,
    CaseMembersV1RevokeTeamCommandValidator,
    CaseMembersV1RevokeTeamCommandHandler,
    CaseMembersV1RevokeTeamCommandResult>
{
    public required Guid CaseId { get; init; }
    public required Guid TeamId { get; init; }
    public required string ActorUserId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

namespace QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Command;

public sealed class CaseMembersV1RevokeTeamCommandResult : ICaseMembersV1RevokeTeamCommandResultQueue
{
    public required Guid CaseTeamMembershipId { get; init; }
}

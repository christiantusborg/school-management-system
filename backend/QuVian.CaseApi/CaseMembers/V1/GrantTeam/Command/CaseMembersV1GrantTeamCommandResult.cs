namespace QuVian.CaseApi.CaseMembers.V1.GrantTeam.Command;

public sealed class CaseMembersV1GrantTeamCommandResult : ICaseMembersV1GrantTeamCommandResultQueue
{
    public required Guid CaseTeamMembershipId { get; init; }
}

namespace QuVian.CaseApi.CaseMembers.V1.GrantUser.Command;

public sealed class CaseMembersV1GrantUserCommandResult : ICaseMembersV1GrantUserCommandResultQueue
{
    public required Guid CaseUserMemberId { get; init; }
}

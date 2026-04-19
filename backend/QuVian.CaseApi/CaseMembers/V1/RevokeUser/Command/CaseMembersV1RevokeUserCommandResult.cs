namespace QuVian.CaseApi.CaseMembers.V1.RevokeUser.Command;

public sealed class CaseMembersV1RevokeUserCommandResult : ICaseMembersV1RevokeUserCommandResultQueue
{
    public required Guid CaseUserMemberId { get; init; }
}

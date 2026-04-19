namespace QuVian.CaseApi.CaseMembers.V1.RevokeUser.Command;

public sealed record CaseMembersV1RevokeUserCommand : IHandleableCommand<
    CaseMembersV1RevokeUserCommand,
    CaseMembersV1RevokeUserCommandValidator,
    CaseMembersV1RevokeUserCommandHandler,
    CaseMembersV1RevokeUserCommandResult>
{
    public required Guid CaseId { get; init; }
    public required string TargetUserId { get; init; }
    public required string ActorUserId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

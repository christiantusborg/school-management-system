namespace QuVian.CaseApi.CaseMembers.V1.List.Command;

public sealed record CaseMembersV1ListCommand : IHandleableCommand<
    CaseMembersV1ListCommand,
    CaseMembersV1ListCommandValidator,
    CaseMembersV1ListCommandHandler,
    CaseMembersV1ListCommandResult>
{
    public required Guid CaseId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

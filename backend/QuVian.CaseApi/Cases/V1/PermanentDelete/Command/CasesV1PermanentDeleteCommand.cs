namespace QuVian.CaseApi.Cases.V1.PermanentDelete.Command;

public sealed record CasesV1PermanentDeleteCommand : IHandleableCommand<
    CasesV1PermanentDeleteCommand,
    CasesV1PermanentDeleteCommandValidator,
    CasesV1PermanentDeleteCommandHandler,
    CasesV1PermanentDeleteCommandResult>
{
    public required Guid CaseId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

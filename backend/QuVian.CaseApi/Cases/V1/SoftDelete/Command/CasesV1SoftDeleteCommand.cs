namespace QuVian.CaseApi.Cases.V1.SoftDelete.Command;

public sealed record CasesV1SoftDeleteCommand : IHandleableCommand<
    CasesV1SoftDeleteCommand,
    CasesV1SoftDeleteCommandValidator,
    CasesV1SoftDeleteCommandHandler,
    CasesV1SoftDeleteCommandResult>
{
    public required Guid CaseId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

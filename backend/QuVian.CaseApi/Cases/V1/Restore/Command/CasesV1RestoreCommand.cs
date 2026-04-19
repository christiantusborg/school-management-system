namespace QuVian.CaseApi.Cases.V1.Restore.Command;

public sealed record CasesV1RestoreCommand : IHandleableCommand<
    CasesV1RestoreCommand,
    CasesV1RestoreCommandValidator,
    CasesV1RestoreCommandHandler,
    CasesV1RestoreCommandResult>
{
    public required Guid CaseId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

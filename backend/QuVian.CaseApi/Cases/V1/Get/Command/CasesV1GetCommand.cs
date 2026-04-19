namespace QuVian.CaseApi.Cases.V1.Get.Command;

public sealed record CasesV1GetCommand : IHandleableCommand<
    CasesV1GetCommand,
    CasesV1GetCommandValidator,
    CasesV1GetCommandHandler,
    CasesV1GetCommandResult>
{
    public required Guid CaseId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

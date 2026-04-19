namespace QuVian.CaseApi.CaseKeyPairs.V1.List.Command;

public sealed record CaseKeyPairsV1ListCommand : IHandleableCommand<
    CaseKeyPairsV1ListCommand,
    CaseKeyPairsV1ListCommandValidator,
    CaseKeyPairsV1ListCommandHandler,
    CaseKeyPairsV1ListCommandResult>
{
    public required Guid CaseId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

namespace QuVian.CaseApi.CaseFiles.V1.List.Command;

public sealed record CaseFilesV1ListCommand : IHandleableCommand<
    CaseFilesV1ListCommand,
    CaseFilesV1ListCommandValidator,
    CaseFilesV1ListCommandHandler,
    CaseFilesV1ListCommandResult>
{
    public required Guid CaseId { get; init; }
    public required string UserId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

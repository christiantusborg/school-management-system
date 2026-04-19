namespace QuVian.CaseApi.CaseFiles.V1.GetKey.Command;

public sealed record CaseFilesV1GetKeyCommand : IHandleableCommand<
    CaseFilesV1GetKeyCommand,
    CaseFilesV1GetKeyCommandValidator,
    CaseFilesV1GetKeyCommandHandler,
    CaseFilesV1GetKeyCommandResult>
{
    public required Guid CaseId { get; init; }
    public required Guid CaseFileId { get; init; }
    public required string UserId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

namespace QuVian.CaseApi.CaseFiles.V1.Create.Command;

public sealed class CaseFilesV1CreateCommandResult : ICaseFilesV1CreateCommandResultQueue
{
    public required Guid CaseFileId { get; init; }
}

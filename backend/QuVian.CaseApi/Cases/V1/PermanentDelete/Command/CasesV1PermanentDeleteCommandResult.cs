namespace QuVian.CaseApi.Cases.V1.PermanentDelete.Command;

public sealed class CasesV1PermanentDeleteCommandResult : ICasesV1PermanentDeleteCommandResultQueue
{
    public required Guid CaseId { get; init; }
}

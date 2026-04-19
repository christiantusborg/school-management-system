namespace QuVian.CaseApi.Cases.V1.Restore.Command;

public sealed class CasesV1RestoreCommandResult : ICasesV1RestoreCommandResultQueue
{
    public required Guid CaseId { get; init; }
}

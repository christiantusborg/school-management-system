namespace QuVian.CaseApi.Cases.V1.Update.Command;

public sealed class CasesV1UpdateCommandResult : ICasesV1UpdateCommandResultQueue
{
    public required Guid CaseId { get; init; }
}

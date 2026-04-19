namespace QuVian.CaseApi.Cases.V1.Create.Command;

public sealed class CasesV1CreateCommandResult : ICasesV1CreateCommandResultQueue
{
    public required Guid CaseId { get; init; }
}

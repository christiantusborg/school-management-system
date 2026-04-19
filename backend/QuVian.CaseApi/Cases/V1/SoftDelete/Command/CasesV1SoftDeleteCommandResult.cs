namespace QuVian.CaseApi.Cases.V1.SoftDelete.Command;

public sealed class CasesV1SoftDeleteCommandResult : ICasesV1SoftDeleteCommandResultQueue
{
    public required Guid CaseId { get; init; }
}

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Command;

public sealed class FinalProjectStatusV1SoftDeleteCommandResult : IFinalProjectStatusV1SoftDeleteCommandResultQueue
{
    public required int FinalProjectStatusId { get; init; }
}

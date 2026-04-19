namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Command;

public sealed class FinalProjectStatusV1PermanentDeleteCommandResult : IFinalProjectStatusV1PermanentDeleteCommandResultQueue
{
    public required int FinalProjectStatusId { get; init; }
}

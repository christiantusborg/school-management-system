namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Command;

public sealed class FinalProjectStatusV1UpdateCommandResult : IFinalProjectStatusV1UpdateCommandResultQueue
{
    public required int FinalProjectStatusId { get; init; }
}

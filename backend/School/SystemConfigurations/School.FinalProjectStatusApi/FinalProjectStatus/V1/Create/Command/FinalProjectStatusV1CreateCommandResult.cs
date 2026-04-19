namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Command;

public sealed class FinalProjectStatusV1CreateCommandResult : IFinalProjectStatusV1CreateCommandResultQueue
{
    public required int FinalProjectStatusId { get; init; }
}

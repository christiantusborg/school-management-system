namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Command;

public sealed class FinalProjectStatusV1RestoreCommandResult : IFinalProjectStatusV1RestoreCommandResultQueue
{
    public required int FinalProjectStatusId { get; init; }
}

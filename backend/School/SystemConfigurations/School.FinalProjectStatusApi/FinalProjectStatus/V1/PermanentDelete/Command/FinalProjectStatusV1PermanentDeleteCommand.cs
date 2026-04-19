namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Command;

public sealed record FinalProjectStatusV1PermanentDeleteCommand : IHandleableCommand<
    FinalProjectStatusV1PermanentDeleteCommand,
    FinalProjectStatusV1PermanentDeleteCommandValidator,
    FinalProjectStatusV1PermanentDeleteCommandHandler,
    FinalProjectStatusV1PermanentDeleteCommandResult>
{
    public required int FinalProjectStatusId { get; init; }
}

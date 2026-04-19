namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Command;

public sealed record FinalProjectStatusV1RestoreCommand : IHandleableCommand<
    FinalProjectStatusV1RestoreCommand,
    FinalProjectStatusV1RestoreCommandValidator,
    FinalProjectStatusV1RestoreCommandHandler,
    FinalProjectStatusV1RestoreCommandResult>
{
    public required int FinalProjectStatusId { get; init; }
}

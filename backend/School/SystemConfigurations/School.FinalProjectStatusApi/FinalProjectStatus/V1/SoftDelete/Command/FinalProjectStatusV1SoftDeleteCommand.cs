namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Command;

public sealed record FinalProjectStatusV1SoftDeleteCommand : IHandleableCommand<
    FinalProjectStatusV1SoftDeleteCommand,
    FinalProjectStatusV1SoftDeleteCommandValidator,
    FinalProjectStatusV1SoftDeleteCommandHandler,
    FinalProjectStatusV1SoftDeleteCommandResult>
{
    public required int FinalProjectStatusId { get; init; }
}

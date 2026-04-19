namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Command;

public sealed record FinalProjectStatusV1GetCommand : IHandleableCommand<
    FinalProjectStatusV1GetCommand,
    FinalProjectStatusV1GetCommandValidator,
    FinalProjectStatusV1GetCommandHandler,
    FinalProjectStatusV1GetCommandResult>
{
    public required int FinalProjectStatusId { get; init; }
}

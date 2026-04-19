namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Command;

public sealed record TuitionFeeStatusV1GetCommand : IHandleableCommand<
    TuitionFeeStatusV1GetCommand,
    TuitionFeeStatusV1GetCommandValidator,
    TuitionFeeStatusV1GetCommandHandler,
    TuitionFeeStatusV1GetCommandResult>
{
    public required int TuitionFeeStatusId { get; init; }
}

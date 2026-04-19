namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Command;

public sealed record TuitionFeeStatusV1PermanentDeleteCommand : IHandleableCommand<
    TuitionFeeStatusV1PermanentDeleteCommand,
    TuitionFeeStatusV1PermanentDeleteCommandValidator,
    TuitionFeeStatusV1PermanentDeleteCommandHandler,
    TuitionFeeStatusV1PermanentDeleteCommandResult>
{
    public required int TuitionFeeStatusId { get; init; }
}

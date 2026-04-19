namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Command;

public sealed record TuitionFeeStatusV1RestoreCommand : IHandleableCommand<
    TuitionFeeStatusV1RestoreCommand,
    TuitionFeeStatusV1RestoreCommandValidator,
    TuitionFeeStatusV1RestoreCommandHandler,
    TuitionFeeStatusV1RestoreCommandResult>
{
    public required int TuitionFeeStatusId { get; init; }
}

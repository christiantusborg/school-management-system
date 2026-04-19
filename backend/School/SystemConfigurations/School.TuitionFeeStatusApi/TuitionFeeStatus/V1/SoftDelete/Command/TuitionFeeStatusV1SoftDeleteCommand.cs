namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Command;

public sealed record TuitionFeeStatusV1SoftDeleteCommand : IHandleableCommand<
    TuitionFeeStatusV1SoftDeleteCommand,
    TuitionFeeStatusV1SoftDeleteCommandValidator,
    TuitionFeeStatusV1SoftDeleteCommandHandler,
    TuitionFeeStatusV1SoftDeleteCommandResult>
{
    public required int TuitionFeeStatusId { get; init; }
}

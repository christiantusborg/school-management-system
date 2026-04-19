namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Command;

public sealed record TuitionFeeStatusV1UpdateCommand : IHandleableCommand<
    TuitionFeeStatusV1UpdateCommand,
    TuitionFeeStatusV1UpdateCommandValidator,
    TuitionFeeStatusV1UpdateCommandHandler,
    TuitionFeeStatusV1UpdateCommandResult>
{
    public required int TuitionFeeStatusId { get; init; }
    public required string Name { get; init; }

}

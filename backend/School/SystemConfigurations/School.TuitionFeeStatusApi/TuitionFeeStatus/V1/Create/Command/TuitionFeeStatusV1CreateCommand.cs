namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Command;

public sealed record TuitionFeeStatusV1CreateCommand : IHandleableCommand<
    TuitionFeeStatusV1CreateCommand,
    TuitionFeeStatusV1CreateCommandValidator,
    TuitionFeeStatusV1CreateCommandHandler,
    TuitionFeeStatusV1CreateCommandResult>
{
    public required string Name { get; init; }

}

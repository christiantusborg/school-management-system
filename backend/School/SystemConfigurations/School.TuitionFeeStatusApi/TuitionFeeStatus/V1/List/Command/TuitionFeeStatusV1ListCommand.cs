namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Command;

public sealed record TuitionFeeStatusV1ListCommand : IHandleableCommand<
    TuitionFeeStatusV1ListCommand,
    TuitionFeeStatusV1ListCommandValidator,
    TuitionFeeStatusV1ListCommandHandler,
    CommandSearchResult<TuitionFeeStatusV1ListCommandResultItem>>;

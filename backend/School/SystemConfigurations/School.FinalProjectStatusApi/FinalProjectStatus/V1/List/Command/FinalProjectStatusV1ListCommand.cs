namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Command;

public sealed record FinalProjectStatusV1ListCommand : IHandleableCommand<
    FinalProjectStatusV1ListCommand,
    FinalProjectStatusV1ListCommandValidator,
    FinalProjectStatusV1ListCommandHandler,
    CommandSearchResult<FinalProjectStatusV1ListCommandResultItem>>;

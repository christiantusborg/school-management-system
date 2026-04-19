namespace School.PathwayApi.Pathway.V1.List.Command;

public sealed record PathwayV1ListCommand : IHandleableCommand<
    PathwayV1ListCommand,
    PathwayV1ListCommandValidator,
    PathwayV1ListCommandHandler,
    CommandSearchResult<PathwayV1ListCommandResultItem>>;

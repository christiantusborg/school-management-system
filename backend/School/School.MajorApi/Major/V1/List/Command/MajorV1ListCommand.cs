namespace School.MajorApi.Major.V1.List.Command;

public sealed record MajorV1ListCommand : IHandleableCommand<
    MajorV1ListCommand,
    MajorV1ListCommandValidator,
    MajorV1ListCommandHandler,
    CommandSearchResult<MajorV1ListCommandResultItem>>
{
    public bool DeletedOnly { get; init; }
}

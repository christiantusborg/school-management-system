namespace School.SubjectApi.Subject.V1.List.Command;

public sealed record SubjectV1ListCommand : IHandleableCommand<
    SubjectV1ListCommand,
    SubjectV1ListCommandValidator,
    SubjectV1ListCommandHandler,
    CommandSearchResult<SubjectV1ListCommandResultItem>>
{
    public bool DeletedOnly { get; init; }
}

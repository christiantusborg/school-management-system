namespace School.ModeOfStudyApi.ModeOfStudy.V1.List.Command;

public sealed record ModeOfStudyV1ListCommand : IHandleableCommand<
    ModeOfStudyV1ListCommand,
    ModeOfStudyV1ListCommandValidator,
    ModeOfStudyV1ListCommandHandler,
    CommandSearchResult<ModeOfStudyV1ListCommandResultItem>>;

namespace School.PathwayApi.EducationLevel.V1.List.Command;

public sealed record EducationLevelV1ListCommand : IHandleableCommand<
    EducationLevelV1ListCommand,
    EducationLevelV1ListCommandValidator,
    EducationLevelV1ListCommandHandler,
    CommandSearchResult<EducationLevelV1ListCommandResultItem>>;

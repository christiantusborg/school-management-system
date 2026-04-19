namespace School.ModeOfStudyApi.ModeOfStudy.V1.Create.Command;

public sealed record ModeOfStudyV1CreateCommand : IHandleableCommand<
    ModeOfStudyV1CreateCommand,
    ModeOfStudyV1CreateCommandValidator,
    ModeOfStudyV1CreateCommandHandler,
    ModeOfStudyV1CreateCommandResult>
{
    public required string Name { get; init; }

}

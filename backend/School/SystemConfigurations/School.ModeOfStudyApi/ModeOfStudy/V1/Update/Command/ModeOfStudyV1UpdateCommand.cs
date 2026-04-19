namespace School.ModeOfStudyApi.ModeOfStudy.V1.Update.Command;

public sealed record ModeOfStudyV1UpdateCommand : IHandleableCommand<
    ModeOfStudyV1UpdateCommand,
    ModeOfStudyV1UpdateCommandValidator,
    ModeOfStudyV1UpdateCommandHandler,
    ModeOfStudyV1UpdateCommandResult>
{
    public required int ModeOfStudyId { get; init; }
    public required string Name { get; init; }

}

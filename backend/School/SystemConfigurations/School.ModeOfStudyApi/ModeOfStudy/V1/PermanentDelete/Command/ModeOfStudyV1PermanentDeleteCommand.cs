namespace School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Command;

public sealed record ModeOfStudyV1PermanentDeleteCommand : IHandleableCommand<
    ModeOfStudyV1PermanentDeleteCommand,
    ModeOfStudyV1PermanentDeleteCommandValidator,
    ModeOfStudyV1PermanentDeleteCommandHandler,
    ModeOfStudyV1PermanentDeleteCommandResult>
{
    public required int ModeOfStudyId { get; init; }
}

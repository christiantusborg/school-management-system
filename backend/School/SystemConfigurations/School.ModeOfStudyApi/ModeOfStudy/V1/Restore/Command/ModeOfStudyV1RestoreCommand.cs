namespace School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Command;

public sealed record ModeOfStudyV1RestoreCommand : IHandleableCommand<
    ModeOfStudyV1RestoreCommand,
    ModeOfStudyV1RestoreCommandValidator,
    ModeOfStudyV1RestoreCommandHandler,
    ModeOfStudyV1RestoreCommandResult>
{
    public required int ModeOfStudyId { get; init; }
}

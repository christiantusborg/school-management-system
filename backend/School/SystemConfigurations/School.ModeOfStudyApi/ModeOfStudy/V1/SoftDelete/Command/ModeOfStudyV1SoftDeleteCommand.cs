namespace School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Command;

public sealed record ModeOfStudyV1SoftDeleteCommand : IHandleableCommand<
    ModeOfStudyV1SoftDeleteCommand,
    ModeOfStudyV1SoftDeleteCommandValidator,
    ModeOfStudyV1SoftDeleteCommandHandler,
    ModeOfStudyV1SoftDeleteCommandResult>
{
    public required int ModeOfStudyId { get; init; }
}

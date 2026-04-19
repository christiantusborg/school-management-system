namespace School.ModeOfStudyApi.ModeOfStudy.V1.Get.Command;

public sealed record ModeOfStudyV1GetCommand : IHandleableCommand<
    ModeOfStudyV1GetCommand,
    ModeOfStudyV1GetCommandValidator,
    ModeOfStudyV1GetCommandHandler,
    ModeOfStudyV1GetCommandResult>
{
    public required int ModeOfStudyId { get; init; }
}

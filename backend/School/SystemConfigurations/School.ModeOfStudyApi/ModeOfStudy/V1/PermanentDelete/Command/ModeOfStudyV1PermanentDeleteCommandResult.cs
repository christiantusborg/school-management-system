namespace School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Command;

public sealed class ModeOfStudyV1PermanentDeleteCommandResult : IModeOfStudyV1PermanentDeleteCommandResultQueue
{
    public required int ModeOfStudyId { get; init; }
}

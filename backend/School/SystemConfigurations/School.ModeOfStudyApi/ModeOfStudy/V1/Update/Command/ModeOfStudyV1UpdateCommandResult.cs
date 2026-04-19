namespace School.ModeOfStudyApi.ModeOfStudy.V1.Update.Command;

public sealed class ModeOfStudyV1UpdateCommandResult : IModeOfStudyV1UpdateCommandResultQueue
{
    public required int ModeOfStudyId { get; init; }
}

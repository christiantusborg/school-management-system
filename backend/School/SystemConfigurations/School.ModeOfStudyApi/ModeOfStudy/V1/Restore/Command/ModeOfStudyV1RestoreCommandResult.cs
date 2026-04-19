namespace School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Command;

public sealed class ModeOfStudyV1RestoreCommandResult : IModeOfStudyV1RestoreCommandResultQueue
{
    public required int ModeOfStudyId { get; init; }
}

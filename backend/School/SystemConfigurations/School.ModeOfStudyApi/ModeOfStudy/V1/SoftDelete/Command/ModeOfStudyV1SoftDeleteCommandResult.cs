namespace School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Command;

public sealed class ModeOfStudyV1SoftDeleteCommandResult : IModeOfStudyV1SoftDeleteCommandResultQueue
{
    public required int ModeOfStudyId { get; init; }
}

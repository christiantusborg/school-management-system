namespace School.ModeOfStudyApi.ModeOfStudy.V1.Create.Command;

public sealed class ModeOfStudyV1CreateCommandResult : IModeOfStudyV1CreateCommandResultQueue
{
    public required int ModeOfStudyId { get; init; }
}

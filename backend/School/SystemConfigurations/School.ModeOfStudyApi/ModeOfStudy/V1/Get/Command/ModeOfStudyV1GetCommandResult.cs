namespace School.ModeOfStudyApi.ModeOfStudy.V1.Get.Command;

public sealed class ModeOfStudyV1GetCommandResult : IModeOfStudyV1GetCommandResultQueue
{
    public required int ModeOfStudyId { get; init; }
    public required string Name { get; init; }
    public DateTime? DeletedAt { get; init; }
}

namespace School.ModeOfStudyApi.ModeOfStudy.V1.List.Command;

public sealed class ModeOfStudyV1ListCommandResultItem : IModeOfStudyV1ListCommandResultItemQueue
{
    public required int ModeOfStudyId { get; init; }
    public required string Name { get; init; }
}

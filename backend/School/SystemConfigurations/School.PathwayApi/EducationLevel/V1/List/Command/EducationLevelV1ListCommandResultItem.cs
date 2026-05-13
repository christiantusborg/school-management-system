namespace School.PathwayApi.EducationLevel.V1.List.Command;

public sealed class EducationLevelV1ListCommandResultItem : IEducationLevelV1ListCommandResultItemQueue
{
    public required Guid EducationLevelId { get; init; }
    public required string Name { get; init; }
    public required int Rank { get; init; }
    public required int DisplayOrder { get; init; }
}

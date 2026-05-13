namespace School.PathwayApi.Pathway.V1.List.Command;

public sealed class PathwayV1ListCommandResultItem : IPathwayV1ListCommandResultItemQueue
{
    public required Guid PathwayId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int MinimumYearsWorkExperience { get; init; }
}

namespace School.PathwayApi.Pathway.V1.List.Command;

public sealed class PathwayV1ListCommandResultItem : IPathwayV1ListCommandResultItemQueue
{
    public required int PathwayId { get; init; }
    public required string Name { get; init; }
}

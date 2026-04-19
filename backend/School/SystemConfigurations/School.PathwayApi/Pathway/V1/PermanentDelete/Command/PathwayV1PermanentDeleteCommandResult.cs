namespace School.PathwayApi.Pathway.V1.PermanentDelete.Command;

public sealed class PathwayV1PermanentDeleteCommandResult : IPathwayV1PermanentDeleteCommandResultQueue
{
    public required int PathwayId { get; init; }
}

namespace School.PathwayApi.Pathway.V1.Update.Command;

public sealed class PathwayV1UpdateCommandResult : IPathwayV1UpdateCommandResultQueue
{
    public required int PathwayId { get; init; }
}

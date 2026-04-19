namespace School.PathwayApi.Pathway.V1.Restore.Command;

public sealed class PathwayV1RestoreCommandResult : IPathwayV1RestoreCommandResultQueue
{
    public required int PathwayId { get; init; }
}

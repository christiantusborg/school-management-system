namespace School.PathwayApi.Pathway.V1.SoftDelete.Command;

public sealed class PathwayV1SoftDeleteCommandResult : IPathwayV1SoftDeleteCommandResultQueue
{
    public required Guid PathwayId { get; init; }
}

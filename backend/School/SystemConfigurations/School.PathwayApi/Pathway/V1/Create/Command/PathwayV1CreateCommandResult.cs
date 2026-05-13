namespace School.PathwayApi.Pathway.V1.Create.Command;

public sealed class PathwayV1CreateCommandResult : IPathwayV1CreateCommandResultQueue
{
    public required Guid PathwayId { get; init; }
}

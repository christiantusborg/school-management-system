namespace School.PathwayApi.Pathway.V1.Get.Command;

public sealed class PathwayV1GetCommandResult : IPathwayV1GetCommandResultQueue
{
    public required int PathwayId { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<int> DocumentTypeIds { get; init; }
    public DateTime? DeletedAt { get; init; }
}

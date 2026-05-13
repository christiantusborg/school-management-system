namespace School.PathwayApi.Pathway.V1.Get.Command;

public sealed class PathwayV1GetCommandResult : IPathwayV1GetCommandResultQueue
{
    public required Guid PathwayId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int MinimumYearsWorkExperience { get; init; }
    public required IReadOnlyList<Guid> DocumentTypeIds { get; init; }
    public required IReadOnlyList<Guid> AcceptedEducationLevelIds { get; init; }
}

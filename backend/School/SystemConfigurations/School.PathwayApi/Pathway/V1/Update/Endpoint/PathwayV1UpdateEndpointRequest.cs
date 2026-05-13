namespace School.PathwayApi.Pathway.V1.Update.Endpoint;

public sealed class PathwayV1UpdateEndpointRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int MinimumYearsWorkExperience { get; init; }
    public IReadOnlyList<Guid>? DocumentTypeIds { get; init; }
    public IReadOnlyList<Guid>? AcceptedEducationLevelIds { get; init; }
}

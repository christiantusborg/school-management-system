namespace School.PathwayApi.Pathway.V1.List.Endpoint;

public sealed class PathwayV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required Guid PathwayId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int MinimumYearsWorkExperience { get; init; }
}

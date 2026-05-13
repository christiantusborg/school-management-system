namespace School.PathwayApi.EducationLevel.V1.Create.Endpoint;

public sealed class EducationLevelV1CreateEndpointRequest
{
    public required string Name { get; init; }
    public required int Rank { get; init; }
    public required int DisplayOrder { get; init; }
}

namespace School.PathwayApi.EducationLevel.V1.Update.Endpoint;

public sealed class EducationLevelV1UpdateEndpointRequest
{
    public required string Name { get; init; }
    public required int Rank { get; init; }
    public required int DisplayOrder { get; init; }
}

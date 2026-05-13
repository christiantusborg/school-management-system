namespace School.PathwayApi.EducationLevel.V1.Create.Endpoint;

public sealed class EducationLevelV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid EducationLevelId { get; init; }
}

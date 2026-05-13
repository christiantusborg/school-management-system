namespace School.PathwayApi.EducationLevel.V1.Update.Endpoint;

public sealed class EducationLevelV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid EducationLevelId { get; init; }
}

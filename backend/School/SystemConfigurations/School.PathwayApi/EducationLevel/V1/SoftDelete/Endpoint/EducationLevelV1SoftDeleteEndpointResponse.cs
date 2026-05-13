namespace School.PathwayApi.EducationLevel.V1.SoftDelete.Endpoint;

public sealed class EducationLevelV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid EducationLevelId { get; init; }
}

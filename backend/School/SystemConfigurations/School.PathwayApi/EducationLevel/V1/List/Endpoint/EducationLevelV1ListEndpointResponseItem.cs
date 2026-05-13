namespace School.PathwayApi.EducationLevel.V1.List.Endpoint;

public sealed class EducationLevelV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required Guid EducationLevelId { get; init; }
    public required string Name { get; init; }
    public required int Rank { get; init; }
    public required int DisplayOrder { get; init; }
}

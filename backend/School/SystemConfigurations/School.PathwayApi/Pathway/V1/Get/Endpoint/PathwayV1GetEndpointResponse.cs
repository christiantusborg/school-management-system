namespace School.PathwayApi.Pathway.V1.Get.Endpoint;

public sealed class PathwayV1GetEndpointResponse : HateoasBaseResponse
{
    public required int PathwayId { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<int> DocumentTypeIds { get; init; }
    public DateTime? DeletedAt { get; init; }
}

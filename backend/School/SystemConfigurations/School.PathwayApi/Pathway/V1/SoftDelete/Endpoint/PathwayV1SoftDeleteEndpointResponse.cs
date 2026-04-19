namespace School.PathwayApi.Pathway.V1.SoftDelete.Endpoint;

public sealed class PathwayV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required int PathwayId { get; init; }
}

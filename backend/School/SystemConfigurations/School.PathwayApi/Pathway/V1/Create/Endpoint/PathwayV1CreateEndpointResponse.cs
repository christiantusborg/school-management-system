namespace School.PathwayApi.Pathway.V1.Create.Endpoint;

public sealed class PathwayV1CreateEndpointResponse : HateoasBaseResponse
{
    public required int PathwayId { get; init; }
}

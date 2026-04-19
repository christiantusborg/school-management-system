namespace School.PathwayApi.Pathway.V1.List.Endpoint;

public sealed class PathwayV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required int PathwayId { get; init; }
    public required string Name { get; init; }

}

namespace School.PathwayApi.Pathway.V1.Update.Endpoint;

public sealed class PathwayV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required int PathwayId { get; init; }
}

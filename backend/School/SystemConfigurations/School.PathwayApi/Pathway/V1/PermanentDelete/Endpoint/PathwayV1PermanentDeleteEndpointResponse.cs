namespace School.PathwayApi.Pathway.V1.PermanentDelete.Endpoint;

public sealed class PathwayV1PermanentDeleteEndpointResponse : HateoasBaseResponse
{
    public required int PathwayId { get; init; }
}

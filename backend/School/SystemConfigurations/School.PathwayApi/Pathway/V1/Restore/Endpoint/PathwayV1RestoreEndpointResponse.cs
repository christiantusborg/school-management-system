namespace School.PathwayApi.Pathway.V1.Restore.Endpoint;

public sealed class PathwayV1RestoreEndpointResponse : HateoasBaseResponse
{
    public required int PathwayId { get; init; }
}

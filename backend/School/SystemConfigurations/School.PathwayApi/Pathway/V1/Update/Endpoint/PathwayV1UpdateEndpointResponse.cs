namespace School.PathwayApi.Pathway.V1.Update.Endpoint;

public sealed class PathwayV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid PathwayId { get; init; }
}

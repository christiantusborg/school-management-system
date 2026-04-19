namespace School.PathwayApi.Pathway.V1.Update.Endpoint;

public sealed class PathwayV1UpdateEndpointRequest
{
    public required string Name { get; init; }
    public IReadOnlyList<int>? DocumentTypeIds { get; init; }
}

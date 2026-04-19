namespace School.PathwayApi.Pathway.V1.Create.Endpoint;

public sealed class PathwayV1CreateEndpointRequest
{
    public required string Name { get; init; }
    public IReadOnlyList<int>? DocumentTypeIds { get; init; }
}

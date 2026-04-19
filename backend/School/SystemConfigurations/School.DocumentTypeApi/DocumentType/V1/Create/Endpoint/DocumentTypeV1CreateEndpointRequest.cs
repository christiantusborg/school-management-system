namespace School.DocumentTypeApi.DocumentType.V1.Create.Endpoint;

public sealed class DocumentTypeV1CreateEndpointRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}

namespace School.DocumentTypeApi.DocumentType.V1.Update.Endpoint;

public sealed class DocumentTypeV1UpdateEndpointRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}

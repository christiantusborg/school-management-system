namespace School.DocumentTypeApi.DocumentType.V1.List.Endpoint;

public sealed class DocumentTypeV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required Guid DocumentTypeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

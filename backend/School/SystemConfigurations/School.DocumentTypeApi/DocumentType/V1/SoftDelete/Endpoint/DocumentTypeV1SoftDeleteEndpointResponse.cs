namespace School.DocumentTypeApi.DocumentType.V1.SoftDelete.Endpoint;

public sealed class DocumentTypeV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required int DocumentTypeId { get; init; }
}

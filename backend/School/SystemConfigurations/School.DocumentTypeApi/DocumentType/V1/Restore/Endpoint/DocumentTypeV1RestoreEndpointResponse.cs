namespace School.DocumentTypeApi.DocumentType.V1.Restore.Endpoint;

public sealed class DocumentTypeV1RestoreEndpointResponse : HateoasBaseResponse
{
    public required int DocumentTypeId { get; init; }
}

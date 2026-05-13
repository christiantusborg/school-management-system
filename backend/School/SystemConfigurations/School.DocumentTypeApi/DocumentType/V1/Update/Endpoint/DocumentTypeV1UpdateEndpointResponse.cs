namespace School.DocumentTypeApi.DocumentType.V1.Update.Endpoint;

public sealed class DocumentTypeV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid DocumentTypeId { get; init; }
}

namespace School.DocumentTypeApi.DocumentType.V1.Create.Endpoint;

public sealed class DocumentTypeV1CreateEndpointResponse : HateoasBaseResponse
{
    public required int DocumentTypeId { get; init; }
}

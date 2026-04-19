namespace School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Endpoint;

public sealed class DocumentTypeV1PermanentDeleteEndpointResponse : HateoasBaseResponse
{
    public required int DocumentTypeId { get; init; }
}

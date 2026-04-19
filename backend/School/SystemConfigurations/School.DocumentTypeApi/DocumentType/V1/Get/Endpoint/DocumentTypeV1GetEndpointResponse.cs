namespace School.DocumentTypeApi.DocumentType.V1.Get.Endpoint;

public sealed class DocumentTypeV1GetEndpointResponse : HateoasBaseResponse
{
    public required int DocumentTypeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public DateTime? DeletedAt { get; init; }
}

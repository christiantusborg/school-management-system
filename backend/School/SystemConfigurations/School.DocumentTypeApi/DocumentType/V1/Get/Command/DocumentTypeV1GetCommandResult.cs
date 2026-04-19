namespace School.DocumentTypeApi.DocumentType.V1.Get.Command;

public sealed class DocumentTypeV1GetCommandResult : IDocumentTypeV1GetCommandResultQueue
{
    public required int DocumentTypeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public DateTime? DeletedAt { get; init; }
}

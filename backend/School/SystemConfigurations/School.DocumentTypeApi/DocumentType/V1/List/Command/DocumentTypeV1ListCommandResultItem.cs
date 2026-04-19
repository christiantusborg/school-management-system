namespace School.DocumentTypeApi.DocumentType.V1.List.Command;

public sealed class DocumentTypeV1ListCommandResultItem : IDocumentTypeV1ListCommandResultItemQueue
{
    public required int DocumentTypeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

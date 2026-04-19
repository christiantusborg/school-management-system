namespace School.DocumentTypeApi.DocumentType.V1.SoftDelete.Command;

public sealed class DocumentTypeV1SoftDeleteCommandResult : IDocumentTypeV1SoftDeleteCommandResultQueue
{
    public required int DocumentTypeId { get; init; }
}

namespace School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Command;

public sealed class DocumentTypeV1PermanentDeleteCommandResult : IDocumentTypeV1PermanentDeleteCommandResultQueue
{
    public required int DocumentTypeId { get; init; }
}

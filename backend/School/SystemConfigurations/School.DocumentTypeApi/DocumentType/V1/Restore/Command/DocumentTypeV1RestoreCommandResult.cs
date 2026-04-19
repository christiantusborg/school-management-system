namespace School.DocumentTypeApi.DocumentType.V1.Restore.Command;

public sealed class DocumentTypeV1RestoreCommandResult : IDocumentTypeV1RestoreCommandResultQueue
{
    public required int DocumentTypeId { get; init; }
}

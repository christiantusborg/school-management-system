namespace School.DocumentTypeApi.DocumentType.V1.Update.Command;

public sealed class DocumentTypeV1UpdateCommandResult : IDocumentTypeV1UpdateCommandResultQueue
{
    public required Guid DocumentTypeId { get; init; }
}

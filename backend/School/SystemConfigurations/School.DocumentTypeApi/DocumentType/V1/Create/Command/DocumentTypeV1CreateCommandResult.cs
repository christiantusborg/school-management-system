namespace School.DocumentTypeApi.DocumentType.V1.Create.Command;

public sealed class DocumentTypeV1CreateCommandResult : IDocumentTypeV1CreateCommandResultQueue
{
    public required Guid DocumentTypeId { get; init; }
}

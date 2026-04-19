namespace School.DocumentTypeApi.DocumentType.V1.SoftDelete.Command;

public sealed record DocumentTypeV1SoftDeleteCommand : IHandleableCommand<
    DocumentTypeV1SoftDeleteCommand,
    DocumentTypeV1SoftDeleteCommandValidator,
    DocumentTypeV1SoftDeleteCommandHandler,
    DocumentTypeV1SoftDeleteCommandResult>
{
    public required int DocumentTypeId { get; init; }
}

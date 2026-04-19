namespace School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Command;

public sealed record DocumentTypeV1PermanentDeleteCommand : IHandleableCommand<
    DocumentTypeV1PermanentDeleteCommand,
    DocumentTypeV1PermanentDeleteCommandValidator,
    DocumentTypeV1PermanentDeleteCommandHandler,
    DocumentTypeV1PermanentDeleteCommandResult>
{
    public required int DocumentTypeId { get; init; }
}

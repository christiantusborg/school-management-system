namespace School.DocumentTypeApi.DocumentType.V1.Restore.Command;

public sealed record DocumentTypeV1RestoreCommand : IHandleableCommand<
    DocumentTypeV1RestoreCommand,
    DocumentTypeV1RestoreCommandValidator,
    DocumentTypeV1RestoreCommandHandler,
    DocumentTypeV1RestoreCommandResult>
{
    public required int DocumentTypeId { get; init; }
}

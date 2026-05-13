namespace School.DocumentTypeApi.DocumentType.V1.Update.Command;

public sealed record DocumentTypeV1UpdateCommand : IHandleableCommand<
    DocumentTypeV1UpdateCommand,
    DocumentTypeV1UpdateCommandValidator,
    DocumentTypeV1UpdateCommandHandler,
    DocumentTypeV1UpdateCommandResult>
{
    public required Guid DocumentTypeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

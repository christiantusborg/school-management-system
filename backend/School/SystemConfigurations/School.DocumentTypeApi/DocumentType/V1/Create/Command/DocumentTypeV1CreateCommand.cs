namespace School.DocumentTypeApi.DocumentType.V1.Create.Command;

public sealed record DocumentTypeV1CreateCommand : IHandleableCommand<
    DocumentTypeV1CreateCommand,
    DocumentTypeV1CreateCommandValidator,
    DocumentTypeV1CreateCommandHandler,
    DocumentTypeV1CreateCommandResult>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}

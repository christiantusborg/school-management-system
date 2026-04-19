namespace School.DocumentTypeApi.DocumentType.V1.List.Command;

public sealed record DocumentTypeV1ListCommand : IHandleableCommand<
    DocumentTypeV1ListCommand,
    DocumentTypeV1ListCommandValidator,
    DocumentTypeV1ListCommandHandler,
    CommandSearchResult<DocumentTypeV1ListCommandResultItem>>;

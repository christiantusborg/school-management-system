namespace School.DocumentTypeApi.DocumentType.V1.Get.Command;

public sealed record DocumentTypeV1GetCommand : IHandleableCommand<
    DocumentTypeV1GetCommand,
    DocumentTypeV1GetCommandValidator,
    DocumentTypeV1GetCommandHandler,
    DocumentTypeV1GetCommandResult>
{
    public required int DocumentTypeId { get; init; }
}

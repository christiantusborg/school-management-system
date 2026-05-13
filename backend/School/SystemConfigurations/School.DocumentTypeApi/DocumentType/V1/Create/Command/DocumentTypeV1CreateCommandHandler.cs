namespace School.DocumentTypeApi.DocumentType.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1CreateCommandHandler(IDocumentTypeRepository repository)
    : ICommandHandler<DocumentTypeV1CreateCommand, DocumentTypeV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.DocumentType, IDocumentTypeRepository>
{
    public Task<SuccessOrFailure<DocumentTypeV1CreateCommandResult>> HandleAsync(
        DocumentTypeV1CreateCommand command, CancellationToken cancellationToken)
    {
        var entity = new SharedLibrary.Basics.Opaque.Domains.DocumentType
        {
            DocumentTypeId = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
        };
        repository.Add(entity);
        return Task.FromResult(new SuccessOrFailure<DocumentTypeV1CreateCommandResult>(
            new DocumentTypeV1CreateCommandResult { DocumentTypeId = entity.DocumentTypeId }));
    }
}

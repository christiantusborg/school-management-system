namespace School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1PermanentDeleteCommandHandler(IDocumentTypeRepository repository)
    : ICommandHandler<DocumentTypeV1PermanentDeleteCommand, DocumentTypeV1PermanentDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.DocumentType, IDocumentTypeRepository>
{
    public async Task<SuccessOrFailure<DocumentTypeV1PermanentDeleteCommandResult>> HandleAsync(
        DocumentTypeV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.DocumentType>()
            .AddWhere(x => x.DocumentTypeId == command.DocumentTypeId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<DocumentTypeV1PermanentDeleteCommandResult>.EntityNotFound(typeof(DocumentTypeV1PermanentDeleteCommand));

        var id = entity.DocumentTypeId;
        repository.Remove(entity);

        return new SuccessOrFailure<DocumentTypeV1PermanentDeleteCommandResult>(
            new DocumentTypeV1PermanentDeleteCommandResult { DocumentTypeId = id });
    }
}

namespace School.DocumentTypeApi.DocumentType.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1RestoreCommandHandler(IDocumentTypeRepository repository)
    : ICommandHandler<DocumentTypeV1RestoreCommand, DocumentTypeV1RestoreCommandResult,
        SharedLibrary.Basics.Opaque.Domains.DocumentType, IDocumentTypeRepository>
{
    public async Task<SuccessOrFailure<DocumentTypeV1RestoreCommandResult>> HandleAsync(
        DocumentTypeV1RestoreCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.DocumentType>()
            .AddWhere(x => x.DocumentTypeId == command.DocumentTypeId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<DocumentTypeV1RestoreCommandResult>.EntityNotFound(typeof(DocumentTypeV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<DocumentTypeV1RestoreCommandResult>(
            new DocumentTypeV1RestoreCommandResult { DocumentTypeId = entity.DocumentTypeId });
    }
}

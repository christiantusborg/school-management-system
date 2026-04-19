namespace School.DocumentTypeApi.DocumentType.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1SoftDeleteCommandHandler(IDocumentTypeRepository repository)
    : ICommandHandler<DocumentTypeV1SoftDeleteCommand, DocumentTypeV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.DocumentType, IDocumentTypeRepository>
{
    public async Task<SuccessOrFailure<DocumentTypeV1SoftDeleteCommandResult>> HandleAsync(
        DocumentTypeV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.DocumentType>()
            .AddWhere(x => x.DocumentTypeId == command.DocumentTypeId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<DocumentTypeV1SoftDeleteCommandResult>.EntityNotFound(typeof(DocumentTypeV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<DocumentTypeV1SoftDeleteCommandResult>(
            new DocumentTypeV1SoftDeleteCommandResult { DocumentTypeId = entity.DocumentTypeId });
    }
}

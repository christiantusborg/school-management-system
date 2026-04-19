namespace School.DocumentTypeApi.DocumentType.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1UpdateCommandHandler(IDocumentTypeRepository repository)
    : ICommandHandler<DocumentTypeV1UpdateCommand, DocumentTypeV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.DocumentType, IDocumentTypeRepository>
{
    public async Task<SuccessOrFailure<DocumentTypeV1UpdateCommandResult>> HandleAsync(
        DocumentTypeV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.DocumentType>()
            .AddWhere(x => x.DocumentTypeId == command.DocumentTypeId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<DocumentTypeV1UpdateCommandResult>.EntityNotFound(typeof(DocumentTypeV1UpdateCommand));

        entity.Name = command.Name;
        entity.Description = command.Description;

        return new SuccessOrFailure<DocumentTypeV1UpdateCommandResult>(
            new DocumentTypeV1UpdateCommandResult { DocumentTypeId = entity.DocumentTypeId });
    }
}

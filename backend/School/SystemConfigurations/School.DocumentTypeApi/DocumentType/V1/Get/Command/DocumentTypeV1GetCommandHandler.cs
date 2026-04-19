namespace School.DocumentTypeApi.DocumentType.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1GetCommandHandler(IDocumentTypeRepository repository)
    : ICommandHandler<DocumentTypeV1GetCommand, DocumentTypeV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.DocumentType, IDocumentTypeRepository>
{
    public async Task<SuccessOrFailure<DocumentTypeV1GetCommandResult>> HandleAsync(
        DocumentTypeV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.DocumentType>()
            .AddWhere(x => x.DocumentTypeId == command.DocumentTypeId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<DocumentTypeV1GetCommandResult>.EntityNotFound(typeof(DocumentTypeV1GetCommand));

        return new SuccessOrFailure<DocumentTypeV1GetCommandResult>(new DocumentTypeV1GetCommandResult
        {
            DocumentTypeId = entity.DocumentTypeId,
            Name = entity.Name,
            Description = entity.Description,
            DeletedAt = entity.DeletedAt
        });
    }
}

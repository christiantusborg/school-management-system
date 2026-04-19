namespace School.DocumentTypeApi.DocumentType.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1ListCommandHandler(IDocumentTypeRepository repository)
    : ICommandHandler<DocumentTypeV1ListCommand, CommandSearchResult<DocumentTypeV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.DocumentType, IDocumentTypeRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<DocumentTypeV1ListCommandResultItem>>> HandleAsync(
        DocumentTypeV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.DocumentType>()
            .AddWhere(x => x.DeletedAt == null);

        var all = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderBy(x => x.Name)
            .Select(x => new DocumentTypeV1ListCommandResultItem
            {
                DocumentTypeId = x.DocumentTypeId,
                Name = x.Name,
                Description = x.Description
            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<DocumentTypeV1ListCommandResultItem>>(
            new CommandSearchResult<DocumentTypeV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

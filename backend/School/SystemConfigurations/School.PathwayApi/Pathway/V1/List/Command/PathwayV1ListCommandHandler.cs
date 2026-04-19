namespace School.PathwayApi.Pathway.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1ListCommandHandler(IPathwayRepository repository)
    : ICommandHandler<PathwayV1ListCommand, CommandSearchResult<PathwayV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<PathwayV1ListCommandResultItem>>> HandleAsync(
        PathwayV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Pathway>()
            .AddWhere(x => x.DeletedAt == null);

        var all = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderBy(x => x.Name)
            .Select(x => new PathwayV1ListCommandResultItem
            {
                PathwayId = x.PathwayId,
                Name = x.Name,

            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<PathwayV1ListCommandResultItem>>(
            new CommandSearchResult<PathwayV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

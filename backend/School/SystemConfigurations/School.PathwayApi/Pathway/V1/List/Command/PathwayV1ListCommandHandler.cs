namespace School.PathwayApi.Pathway.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1ListCommandHandler(OdinDbContext db)
    : ICommandHandler<PathwayV1ListCommand, CommandSearchResult<PathwayV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<PathwayV1ListCommandResultItem>>> HandleAsync(
        PathwayV1ListCommand command, CancellationToken cancellationToken)
    {
        var items = await db.Pathways
            .Where(x => x.DeletedAt == null)
            .OrderBy(x => x.Name)
            .Select(x => new PathwayV1ListCommandResultItem
            {
                PathwayId = x.PathwayId,
                Name = x.Name,
                Description = x.Description,
                MinimumYearsWorkExperience = x.MinimumYearsWorkExperience,
            })
            .ToListAsync(cancellationToken);

        return new SuccessOrFailure<CommandSearchResult<PathwayV1ListCommandResultItem>>(
            new CommandSearchResult<PathwayV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

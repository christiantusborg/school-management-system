namespace School.PathwayApi.EducationLevel.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EducationLevelV1ListCommandHandler(OdinDbContext db)
    : ICommandHandler<EducationLevelV1ListCommand, CommandSearchResult<EducationLevelV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.EducationLevel, IEducationLevelRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<EducationLevelV1ListCommandResultItem>>> HandleAsync(
        EducationLevelV1ListCommand command, CancellationToken cancellationToken)
    {
        var items = await db.EducationLevels
            .Where(x => x.DeletedAt == null)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new EducationLevelV1ListCommandResultItem
            {
                EducationLevelId = x.EducationLevelId,
                Name = x.Name,
                Rank = x.Rank,
                DisplayOrder = x.DisplayOrder,
            })
            .ToListAsync(cancellationToken);

        return new SuccessOrFailure<CommandSearchResult<EducationLevelV1ListCommandResultItem>>(
            new CommandSearchResult<EducationLevelV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

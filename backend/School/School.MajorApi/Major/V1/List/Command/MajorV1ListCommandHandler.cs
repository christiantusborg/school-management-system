namespace School.MajorApi.Major.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1ListCommandHandler(IMajorRepository repository)
    : ICommandHandler<MajorV1ListCommand, CommandSearchResult<MajorV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.Major, IMajorRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<MajorV1ListCommandResultItem>>> HandleAsync(
        MajorV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Major>()
            .AddWhere(x => command.DeletedOnly ? x.DeletedAt != null : x.DeletedAt == null);

        var all = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderBy(x => x.Name)
            .Select(x => new MajorV1ListCommandResultItem
            {
                MajorId = x.MajorId,
                ProgrammeId = x.ProgrammeId,
                Name = x.Name,
                DeletedAt = x.DeletedAt,
            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<MajorV1ListCommandResultItem>>(
            new CommandSearchResult<MajorV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

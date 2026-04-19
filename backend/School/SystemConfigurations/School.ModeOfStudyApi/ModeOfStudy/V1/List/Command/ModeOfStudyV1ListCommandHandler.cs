namespace School.ModeOfStudyApi.ModeOfStudy.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1ListCommandHandler(IModeOfStudyRepository repository)
    : ICommandHandler<ModeOfStudyV1ListCommand, CommandSearchResult<ModeOfStudyV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.ModeOfStudy, IModeOfStudyRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<ModeOfStudyV1ListCommandResultItem>>> HandleAsync(
        ModeOfStudyV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.ModeOfStudy>()
            .AddWhere(x => x.DeletedAt == null);

        var all = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderBy(x => x.Name)
            .Select(x => new ModeOfStudyV1ListCommandResultItem
            {
                ModeOfStudyId = x.ModeOfStudyId,
                Name = x.Name,

            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<ModeOfStudyV1ListCommandResultItem>>(
            new CommandSearchResult<ModeOfStudyV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

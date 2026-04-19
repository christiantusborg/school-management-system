namespace School.SubjectApi.Subject.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1ListCommandHandler(ISubjectRepository repository)
    : ICommandHandler<SubjectV1ListCommand, CommandSearchResult<SubjectV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.Subject, ISubjectRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<SubjectV1ListCommandResultItem>>> HandleAsync(
        SubjectV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Subject>()
            .AddWhere(x => command.DeletedOnly ? x.DeletedAt != null : x.DeletedAt == null);

        var all = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderBy(x => x.Name)
            .Select(x => new SubjectV1ListCommandResultItem
            {
                SubjectId = x.SubjectId,
                MajorId = x.MajorId,
                Code = x.Code,
                Name = x.Name,
                Ects = x.Ects,
                DeletedAt = x.DeletedAt,
            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<SubjectV1ListCommandResultItem>>(
            new CommandSearchResult<SubjectV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

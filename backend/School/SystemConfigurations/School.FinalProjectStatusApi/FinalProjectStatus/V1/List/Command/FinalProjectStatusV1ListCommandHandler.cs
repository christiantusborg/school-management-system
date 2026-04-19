namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1ListCommandHandler(IFinalProjectStatusRepository repository)
    : ICommandHandler<FinalProjectStatusV1ListCommand, CommandSearchResult<FinalProjectStatusV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus, IFinalProjectStatusRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<FinalProjectStatusV1ListCommandResultItem>>> HandleAsync(
        FinalProjectStatusV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus>()
            .AddWhere(x => x.DeletedAt == null);

        var all = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderBy(x => x.Name)
            .Select(x => new FinalProjectStatusV1ListCommandResultItem
            {
                FinalProjectStatusId = x.FinalProjectStatusId,
                Name = x.Name,
                Description = x.Description,
                AllowSetByPartner = x.AllowSetByPartner,
            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<FinalProjectStatusV1ListCommandResultItem>>(
            new CommandSearchResult<FinalProjectStatusV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

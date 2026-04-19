namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1ListCommandHandler(ITuitionFeeStatusRepository repository)
    : ICommandHandler<TuitionFeeStatusV1ListCommand, CommandSearchResult<TuitionFeeStatusV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus, ITuitionFeeStatusRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<TuitionFeeStatusV1ListCommandResultItem>>> HandleAsync(
        TuitionFeeStatusV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus>()
            .AddWhere(x => x.DeletedAt == null);

        var all = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderBy(x => x.Name)
            .Select(x => new TuitionFeeStatusV1ListCommandResultItem
            {
                TuitionFeeStatusId = x.TuitionFeeStatusId,
                Name = x.Name,

            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<TuitionFeeStatusV1ListCommandResultItem>>(
            new CommandSearchResult<TuitionFeeStatusV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

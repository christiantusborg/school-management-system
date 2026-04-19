namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1ListCommandHandler(IEnrollmentStatusRepository repository)
    : ICommandHandler<EnrollmentStatusV1ListCommand, CommandSearchResult<EnrollmentStatusV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus, IEnrollmentStatusRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<EnrollmentStatusV1ListCommandResultItem>>> HandleAsync(
        EnrollmentStatusV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus>()
            .AddWhere(x => x.DeletedAt == null);

        var all = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderBy(x => x.Name)
            .Select(x => new EnrollmentStatusV1ListCommandResultItem
            {
                EnrollmentStatusId = x.EnrollmentStatusId,
                Name = x.Name,
                AllowSetByPartner = x.AllowSetByPartner,
            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<EnrollmentStatusV1ListCommandResultItem>>(
            new CommandSearchResult<EnrollmentStatusV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}

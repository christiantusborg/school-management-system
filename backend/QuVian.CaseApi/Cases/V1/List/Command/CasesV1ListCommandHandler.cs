namespace QuVian.CaseApi.Cases.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1ListCommandHandler(ICaseRepository repository)
    : ICommandHandler<CasesV1ListCommand, CommandSearchResult<CasesV1ListCommandResultItem>, Case, ICaseRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<CasesV1ListCommandResultItem>>> HandleAsync(
        CasesV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<Case>()
            .AddWhere(x => x.TenantId == command.TenantId)
            .AddWhere(x => x.DeletedAt == null);

        if (command.Status.HasValue)
            spec.AddWhere(x => x.Status == command.Status.Value);

        if (command.Priority.HasValue)
            spec.AddWhere(x => x.Priority == command.Priority.Value);

        var all = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);
        var total = all.Count;

        var items = all
            .OrderByDescending(x => x.Priority)
            .Skip((command.Page - 1) * command.PageSize)
            .Take(command.PageSize)
            .Select(x => new CasesV1ListCommandResultItem
            {
                CaseId = x.CaseId,
                Name = x.Name,
                Description = x.Description,
                Status = x.Status,
                Priority = x.Priority,
                DueDate = x.DueDate,
                CreatedByUserId = x.CreatedByUserId
            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<CasesV1ListCommandResultItem>>(
            new CommandSearchResult<CasesV1ListCommandResultItem> { Items = items, Total = total });
    }
}

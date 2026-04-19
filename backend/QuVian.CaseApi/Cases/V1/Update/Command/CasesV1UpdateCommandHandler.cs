namespace QuVian.CaseApi.Cases.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1UpdateCommandHandler(ICaseRepository repository)
    : ICommandHandler<CasesV1UpdateCommand, CasesV1UpdateCommandResult, Case, ICaseRepository>
{
    public async Task<SuccessOrFailure<CasesV1UpdateCommandResult>> HandleAsync(
        CasesV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<Case>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.TenantId == command.TenantId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<CasesV1UpdateCommandResult>.EntityNotFound(typeof(CasesV1UpdateCommand));

        entity.Name = command.Name;
        entity.Description = command.Description;
        entity.Status = command.Status;
        entity.Priority = command.Priority;
        entity.DueDate = command.DueDate;

        return new SuccessOrFailure<CasesV1UpdateCommandResult>(
            new CasesV1UpdateCommandResult { CaseId = entity.CaseId });
    }
}

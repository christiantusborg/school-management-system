namespace QuVian.CaseApi.Cases.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1PermanentDeleteCommandHandler(ICaseRepository repository)
    : ICommandHandler<CasesV1PermanentDeleteCommand, CasesV1PermanentDeleteCommandResult, Case, ICaseRepository>
{
    public async Task<SuccessOrFailure<CasesV1PermanentDeleteCommandResult>> HandleAsync(
        CasesV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        // Allow permanent delete of both active and soft-deleted cases
        var spec = new Specification<Case>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.TenantId == command.TenantId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<CasesV1PermanentDeleteCommandResult>.EntityNotFound(typeof(CasesV1PermanentDeleteCommand));

        var caseId = entity.CaseId;
        repository.Remove(entity);

        return new SuccessOrFailure<CasesV1PermanentDeleteCommandResult>(
            new CasesV1PermanentDeleteCommandResult { CaseId = caseId });
    }
}

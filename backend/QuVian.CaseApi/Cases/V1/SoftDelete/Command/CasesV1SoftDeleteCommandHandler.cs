namespace QuVian.CaseApi.Cases.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1SoftDeleteCommandHandler(ICaseRepository repository)
    : ICommandHandler<CasesV1SoftDeleteCommand, CasesV1SoftDeleteCommandResult, Case, ICaseRepository>
{
    public async Task<SuccessOrFailure<CasesV1SoftDeleteCommandResult>> HandleAsync(
        CasesV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<Case>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.TenantId == command.TenantId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<CasesV1SoftDeleteCommandResult>.EntityNotFound(typeof(CasesV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<CasesV1SoftDeleteCommandResult>(
            new CasesV1SoftDeleteCommandResult { CaseId = entity.CaseId });
    }
}

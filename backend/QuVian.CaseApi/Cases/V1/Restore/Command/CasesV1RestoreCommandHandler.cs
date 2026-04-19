namespace QuVian.CaseApi.Cases.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1RestoreCommandHandler(ICaseRepository repository)
    : ICommandHandler<CasesV1RestoreCommand, CasesV1RestoreCommandResult, Case, ICaseRepository>
{
    public async Task<SuccessOrFailure<CasesV1RestoreCommandResult>> HandleAsync(
        CasesV1RestoreCommand command, CancellationToken cancellationToken)
    {
        // Include soft-deleted records by NOT filtering DeletedAt == null
        var spec = new Specification<Case>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.TenantId == command.TenantId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<CasesV1RestoreCommandResult>.EntityNotFound(typeof(CasesV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<CasesV1RestoreCommandResult>(
            new CasesV1RestoreCommandResult { CaseId = entity.CaseId });
    }
}

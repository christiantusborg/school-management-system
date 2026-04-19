namespace QuVian.CaseApi.Cases.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1GetCommandHandler(ICaseRepository repository)
    : ICommandHandler<CasesV1GetCommand, CasesV1GetCommandResult, Case, ICaseRepository>
{
    public async Task<SuccessOrFailure<CasesV1GetCommandResult>> HandleAsync(
        CasesV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<Case>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.TenantId == command.TenantId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<CasesV1GetCommandResult>.EntityNotFound(typeof(CasesV1GetCommand));

        return new SuccessOrFailure<CasesV1GetCommandResult>(new CasesV1GetCommandResult
        {
            CaseId = entity.CaseId,
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status,
            Priority = entity.Priority,
            DueDate = entity.DueDate,
            CreatedByUserId = entity.CreatedByUserId,
            DeletedAt = entity.DeletedAt
        });
    }
}

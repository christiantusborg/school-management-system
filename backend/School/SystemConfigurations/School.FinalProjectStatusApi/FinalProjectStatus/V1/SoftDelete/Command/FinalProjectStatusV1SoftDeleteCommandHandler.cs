namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1SoftDeleteCommandHandler(IFinalProjectStatusRepository repository)
    : ICommandHandler<FinalProjectStatusV1SoftDeleteCommand, FinalProjectStatusV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus, IFinalProjectStatusRepository>
{
    public async Task<SuccessOrFailure<FinalProjectStatusV1SoftDeleteCommandResult>> HandleAsync(
        FinalProjectStatusV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus>()
            .AddWhere(x => x.FinalProjectStatusId == command.FinalProjectStatusId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<FinalProjectStatusV1SoftDeleteCommandResult>.EntityNotFound(typeof(FinalProjectStatusV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<FinalProjectStatusV1SoftDeleteCommandResult>(
            new FinalProjectStatusV1SoftDeleteCommandResult { FinalProjectStatusId = entity.FinalProjectStatusId });
    }
}

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1PermanentDeleteCommandHandler(IFinalProjectStatusRepository repository)
    : ICommandHandler<FinalProjectStatusV1PermanentDeleteCommand, FinalProjectStatusV1PermanentDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus, IFinalProjectStatusRepository>
{
    public async Task<SuccessOrFailure<FinalProjectStatusV1PermanentDeleteCommandResult>> HandleAsync(
        FinalProjectStatusV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus>()
            .AddWhere(x => x.FinalProjectStatusId == command.FinalProjectStatusId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<FinalProjectStatusV1PermanentDeleteCommandResult>.EntityNotFound(typeof(FinalProjectStatusV1PermanentDeleteCommand));

        var id = entity.FinalProjectStatusId;
        repository.Remove(entity);

        return new SuccessOrFailure<FinalProjectStatusV1PermanentDeleteCommandResult>(
            new FinalProjectStatusV1PermanentDeleteCommandResult { FinalProjectStatusId = id });
    }
}

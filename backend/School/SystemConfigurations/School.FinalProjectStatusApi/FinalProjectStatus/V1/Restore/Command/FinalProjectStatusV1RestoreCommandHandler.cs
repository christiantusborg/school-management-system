namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1RestoreCommandHandler(IFinalProjectStatusRepository repository)
    : ICommandHandler<FinalProjectStatusV1RestoreCommand, FinalProjectStatusV1RestoreCommandResult,
        SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus, IFinalProjectStatusRepository>
{
    public async Task<SuccessOrFailure<FinalProjectStatusV1RestoreCommandResult>> HandleAsync(
        FinalProjectStatusV1RestoreCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus>()
            .AddWhere(x => x.FinalProjectStatusId == command.FinalProjectStatusId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<FinalProjectStatusV1RestoreCommandResult>.EntityNotFound(typeof(FinalProjectStatusV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<FinalProjectStatusV1RestoreCommandResult>(
            new FinalProjectStatusV1RestoreCommandResult { FinalProjectStatusId = entity.FinalProjectStatusId });
    }
}

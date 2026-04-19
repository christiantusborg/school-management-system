namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1RestoreCommandHandler(ITuitionFeeStatusRepository repository)
    : ICommandHandler<TuitionFeeStatusV1RestoreCommand, TuitionFeeStatusV1RestoreCommandResult,
        SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus, ITuitionFeeStatusRepository>
{
    public async Task<SuccessOrFailure<TuitionFeeStatusV1RestoreCommandResult>> HandleAsync(
        TuitionFeeStatusV1RestoreCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus>()
            .AddWhere(x => x.TuitionFeeStatusId == command.TuitionFeeStatusId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<TuitionFeeStatusV1RestoreCommandResult>.EntityNotFound(typeof(TuitionFeeStatusV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<TuitionFeeStatusV1RestoreCommandResult>(
            new TuitionFeeStatusV1RestoreCommandResult { TuitionFeeStatusId = entity.TuitionFeeStatusId });
    }
}

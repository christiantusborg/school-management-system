namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1PermanentDeleteCommandHandler(ITuitionFeeStatusRepository repository)
    : ICommandHandler<TuitionFeeStatusV1PermanentDeleteCommand, TuitionFeeStatusV1PermanentDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus, ITuitionFeeStatusRepository>
{
    public async Task<SuccessOrFailure<TuitionFeeStatusV1PermanentDeleteCommandResult>> HandleAsync(
        TuitionFeeStatusV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus>()
            .AddWhere(x => x.TuitionFeeStatusId == command.TuitionFeeStatusId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<TuitionFeeStatusV1PermanentDeleteCommandResult>.EntityNotFound(typeof(TuitionFeeStatusV1PermanentDeleteCommand));

        var id = entity.TuitionFeeStatusId;
        repository.Remove(entity);

        return new SuccessOrFailure<TuitionFeeStatusV1PermanentDeleteCommandResult>(
            new TuitionFeeStatusV1PermanentDeleteCommandResult { TuitionFeeStatusId = id });
    }
}

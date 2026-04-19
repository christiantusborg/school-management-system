namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1SoftDeleteCommandHandler(ITuitionFeeStatusRepository repository)
    : ICommandHandler<TuitionFeeStatusV1SoftDeleteCommand, TuitionFeeStatusV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus, ITuitionFeeStatusRepository>
{
    public async Task<SuccessOrFailure<TuitionFeeStatusV1SoftDeleteCommandResult>> HandleAsync(
        TuitionFeeStatusV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus>()
            .AddWhere(x => x.TuitionFeeStatusId == command.TuitionFeeStatusId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<TuitionFeeStatusV1SoftDeleteCommandResult>.EntityNotFound(typeof(TuitionFeeStatusV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<TuitionFeeStatusV1SoftDeleteCommandResult>(
            new TuitionFeeStatusV1SoftDeleteCommandResult { TuitionFeeStatusId = entity.TuitionFeeStatusId });
    }
}

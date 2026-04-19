namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1UpdateCommandHandler(ITuitionFeeStatusRepository repository)
    : ICommandHandler<TuitionFeeStatusV1UpdateCommand, TuitionFeeStatusV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus, ITuitionFeeStatusRepository>
{
    public async Task<SuccessOrFailure<TuitionFeeStatusV1UpdateCommandResult>> HandleAsync(
        TuitionFeeStatusV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus>()
            .AddWhere(x => x.TuitionFeeStatusId == command.TuitionFeeStatusId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<TuitionFeeStatusV1UpdateCommandResult>.EntityNotFound(typeof(TuitionFeeStatusV1UpdateCommand));

        entity.Name = command.Name;


        return new SuccessOrFailure<TuitionFeeStatusV1UpdateCommandResult>(
            new TuitionFeeStatusV1UpdateCommandResult { TuitionFeeStatusId = entity.TuitionFeeStatusId });
    }
}

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1GetCommandHandler(ITuitionFeeStatusRepository repository)
    : ICommandHandler<TuitionFeeStatusV1GetCommand, TuitionFeeStatusV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus, ITuitionFeeStatusRepository>
{
    public async Task<SuccessOrFailure<TuitionFeeStatusV1GetCommandResult>> HandleAsync(
        TuitionFeeStatusV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus>()
            .AddWhere(x => x.TuitionFeeStatusId == command.TuitionFeeStatusId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<TuitionFeeStatusV1GetCommandResult>.EntityNotFound(typeof(TuitionFeeStatusV1GetCommand));

        return new SuccessOrFailure<TuitionFeeStatusV1GetCommandResult>(new TuitionFeeStatusV1GetCommandResult
        {
            TuitionFeeStatusId = entity.TuitionFeeStatusId,
            Name = entity.Name,

            DeletedAt = entity.DeletedAt
        });
    }
}

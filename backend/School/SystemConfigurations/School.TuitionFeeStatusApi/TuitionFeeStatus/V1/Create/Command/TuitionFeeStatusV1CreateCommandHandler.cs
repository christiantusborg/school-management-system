namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class TuitionFeeStatusV1CreateCommandHandler(ITuitionFeeStatusRepository repository)
    : ICommandHandler<TuitionFeeStatusV1CreateCommand, TuitionFeeStatusV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus, ITuitionFeeStatusRepository>
{
    public async Task<SuccessOrFailure<TuitionFeeStatusV1CreateCommandResult>> HandleAsync(
        TuitionFeeStatusV1CreateCommand command, CancellationToken cancellationToken)
    {
        var entity = new SharedLibrary.Basics.Opaque.Domains.TuitionFeeStatus
        {
            Name = command.Name,

        };
        repository.Add(entity);
        return new SuccessOrFailure<TuitionFeeStatusV1CreateCommandResult>(
            new TuitionFeeStatusV1CreateCommandResult { TuitionFeeStatusId = entity.TuitionFeeStatusId });
    }
}

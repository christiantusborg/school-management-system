namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1CreateCommandHandler(IFinalProjectStatusRepository repository)
    : ICommandHandler<FinalProjectStatusV1CreateCommand, FinalProjectStatusV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus, IFinalProjectStatusRepository>
{
    public async Task<SuccessOrFailure<FinalProjectStatusV1CreateCommandResult>> HandleAsync(
        FinalProjectStatusV1CreateCommand command, CancellationToken cancellationToken)
    {
        var entity = new SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus
        {
            Name = command.Name,
            Description = command.Description,
            AllowSetByPartner = command.AllowSetByPartner,
        };
        repository.Add(entity);
        return new SuccessOrFailure<FinalProjectStatusV1CreateCommandResult>(
            new FinalProjectStatusV1CreateCommandResult { FinalProjectStatusId = entity.FinalProjectStatusId });
    }
}

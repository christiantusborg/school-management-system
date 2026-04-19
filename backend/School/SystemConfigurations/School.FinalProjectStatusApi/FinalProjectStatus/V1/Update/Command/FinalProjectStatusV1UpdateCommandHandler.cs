namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1UpdateCommandHandler(IFinalProjectStatusRepository repository)
    : ICommandHandler<FinalProjectStatusV1UpdateCommand, FinalProjectStatusV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus, IFinalProjectStatusRepository>
{
    public async Task<SuccessOrFailure<FinalProjectStatusV1UpdateCommandResult>> HandleAsync(
        FinalProjectStatusV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus>()
            .AddWhere(x => x.FinalProjectStatusId == command.FinalProjectStatusId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<FinalProjectStatusV1UpdateCommandResult>.EntityNotFound(typeof(FinalProjectStatusV1UpdateCommand));

        entity.Name = command.Name;
        entity.Description = command.Description;
        entity.AllowSetByPartner = command.AllowSetByPartner;

        return new SuccessOrFailure<FinalProjectStatusV1UpdateCommandResult>(
            new FinalProjectStatusV1UpdateCommandResult { FinalProjectStatusId = entity.FinalProjectStatusId });
    }
}

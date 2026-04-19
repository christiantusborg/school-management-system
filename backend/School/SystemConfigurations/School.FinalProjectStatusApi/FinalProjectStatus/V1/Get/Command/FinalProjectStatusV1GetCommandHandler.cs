namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1GetCommandHandler(IFinalProjectStatusRepository repository)
    : ICommandHandler<FinalProjectStatusV1GetCommand, FinalProjectStatusV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus, IFinalProjectStatusRepository>
{
    public async Task<SuccessOrFailure<FinalProjectStatusV1GetCommandResult>> HandleAsync(
        FinalProjectStatusV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.FinalProjectStatus>()
            .AddWhere(x => x.FinalProjectStatusId == command.FinalProjectStatusId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<FinalProjectStatusV1GetCommandResult>.EntityNotFound(typeof(FinalProjectStatusV1GetCommand));

        return new SuccessOrFailure<FinalProjectStatusV1GetCommandResult>(new FinalProjectStatusV1GetCommandResult
        {
            FinalProjectStatusId = entity.FinalProjectStatusId,
            Name = entity.Name,
            Description = entity.Description,
            AllowSetByPartner = entity.AllowSetByPartner,
            DeletedAt = entity.DeletedAt
        });
    }
}

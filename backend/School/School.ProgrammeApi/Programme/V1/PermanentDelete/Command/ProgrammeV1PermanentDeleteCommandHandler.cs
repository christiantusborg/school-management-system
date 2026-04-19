namespace School.ProgrammeApi.Programme.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1PermanentDeleteCommandHandler(IProgrammeRepository repository)
    : ICommandHandler<ProgrammeV1PermanentDeleteCommand, ProgrammeV1PermanentDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Programme, IProgrammeRepository>
{
    public async Task<SuccessOrFailure<ProgrammeV1PermanentDeleteCommandResult>> HandleAsync(
        ProgrammeV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Programme>()
            .AddWhere(x => x.ProgrammeId == command.ProgrammeId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ProgrammeV1PermanentDeleteCommandResult>.EntityNotFound(typeof(ProgrammeV1PermanentDeleteCommand));

        var savedId = entity.ProgrammeId;
        repository.Remove(entity);

        return new SuccessOrFailure<ProgrammeV1PermanentDeleteCommandResult>(
            new ProgrammeV1PermanentDeleteCommandResult { ProgrammeId = savedId });
    }
}

namespace School.ProgrammeApi.Programme.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1RestoreCommandHandler(IProgrammeRepository repository)
    : ICommandHandler<ProgrammeV1RestoreCommand, ProgrammeV1RestoreCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Programme, IProgrammeRepository>
{
    public async Task<SuccessOrFailure<ProgrammeV1RestoreCommandResult>> HandleAsync(
        ProgrammeV1RestoreCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Programme>()
            .AddWhere(x => x.ProgrammeId == command.ProgrammeId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ProgrammeV1RestoreCommandResult>.EntityNotFound(typeof(ProgrammeV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<ProgrammeV1RestoreCommandResult>(
            new ProgrammeV1RestoreCommandResult { ProgrammeId = entity.ProgrammeId });
    }
}

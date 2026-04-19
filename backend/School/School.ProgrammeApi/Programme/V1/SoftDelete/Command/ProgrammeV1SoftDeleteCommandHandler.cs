namespace School.ProgrammeApi.Programme.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1SoftDeleteCommandHandler(IProgrammeRepository repository)
    : ICommandHandler<ProgrammeV1SoftDeleteCommand, ProgrammeV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Programme, IProgrammeRepository>
{
    public async Task<SuccessOrFailure<ProgrammeV1SoftDeleteCommandResult>> HandleAsync(
        ProgrammeV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Programme>()
            .AddWhere(x => x.ProgrammeId == command.ProgrammeId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ProgrammeV1SoftDeleteCommandResult>.EntityNotFound(typeof(ProgrammeV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<ProgrammeV1SoftDeleteCommandResult>(
            new ProgrammeV1SoftDeleteCommandResult { ProgrammeId = entity.ProgrammeId });
    }
}

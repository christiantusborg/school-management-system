namespace School.MajorApi.Major.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1RestoreCommandHandler(IMajorRepository repository)
    : ICommandHandler<MajorV1RestoreCommand, MajorV1RestoreCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Major, IMajorRepository>
{
    public async Task<SuccessOrFailure<MajorV1RestoreCommandResult>> HandleAsync(
        MajorV1RestoreCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Major>()
            .AddWhere(x => x.MajorId == command.MajorId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<MajorV1RestoreCommandResult>.EntityNotFound(typeof(MajorV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<MajorV1RestoreCommandResult>(
            new MajorV1RestoreCommandResult { MajorId = entity.MajorId });
    }
}

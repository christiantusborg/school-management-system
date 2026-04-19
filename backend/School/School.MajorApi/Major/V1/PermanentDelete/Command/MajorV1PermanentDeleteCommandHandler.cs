namespace School.MajorApi.Major.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1PermanentDeleteCommandHandler(IMajorRepository repository)
    : ICommandHandler<MajorV1PermanentDeleteCommand, MajorV1PermanentDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Major, IMajorRepository>
{
    public async Task<SuccessOrFailure<MajorV1PermanentDeleteCommandResult>> HandleAsync(
        MajorV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Major>()
            .AddWhere(x => x.MajorId == command.MajorId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<MajorV1PermanentDeleteCommandResult>.EntityNotFound(typeof(MajorV1PermanentDeleteCommand));

        var savedId = entity.MajorId;
        repository.Remove(entity);

        return new SuccessOrFailure<MajorV1PermanentDeleteCommandResult>(
            new MajorV1PermanentDeleteCommandResult { MajorId = savedId });
    }
}

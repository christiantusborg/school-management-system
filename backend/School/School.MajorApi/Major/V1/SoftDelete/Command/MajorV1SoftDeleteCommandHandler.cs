namespace School.MajorApi.Major.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1SoftDeleteCommandHandler(IMajorRepository repository)
    : ICommandHandler<MajorV1SoftDeleteCommand, MajorV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Major, IMajorRepository>
{
    public async Task<SuccessOrFailure<MajorV1SoftDeleteCommandResult>> HandleAsync(
        MajorV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Major>()
            .AddWhere(x => x.MajorId == command.MajorId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<MajorV1SoftDeleteCommandResult>.EntityNotFound(typeof(MajorV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<MajorV1SoftDeleteCommandResult>(
            new MajorV1SoftDeleteCommandResult { MajorId = entity.MajorId });
    }
}

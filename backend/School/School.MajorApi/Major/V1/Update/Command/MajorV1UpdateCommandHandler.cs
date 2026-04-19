namespace School.MajorApi.Major.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1UpdateCommandHandler(IMajorRepository repository)
    : ICommandHandler<MajorV1UpdateCommand, MajorV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Major, IMajorRepository>
{
    public async Task<SuccessOrFailure<MajorV1UpdateCommandResult>> HandleAsync(
        MajorV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Major>()
            .AddWhere(x => x.MajorId == command.MajorId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<MajorV1UpdateCommandResult>.EntityNotFound(typeof(MajorV1UpdateCommand));

        entity.ProgrammeId = command.ProgrammeId;
        entity.Name = command.Name;

        return new SuccessOrFailure<MajorV1UpdateCommandResult>(
            new MajorV1UpdateCommandResult { MajorId = entity.MajorId });
    }
}

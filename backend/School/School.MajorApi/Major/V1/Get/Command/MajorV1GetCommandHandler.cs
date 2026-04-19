namespace School.MajorApi.Major.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1GetCommandHandler(IMajorRepository repository)
    : ICommandHandler<MajorV1GetCommand, MajorV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Major, IMajorRepository>
{
    public async Task<SuccessOrFailure<MajorV1GetCommandResult>> HandleAsync(
        MajorV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Major>()
            .AddWhere(x => x.MajorId == command.MajorId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<MajorV1GetCommandResult>.EntityNotFound(typeof(MajorV1GetCommand));

        return new SuccessOrFailure<MajorV1GetCommandResult>(new MajorV1GetCommandResult
        {
            MajorId = entity.MajorId,
            ProgrammeId = entity.ProgrammeId,
            Name = entity.Name,
            DeletedAt = entity.DeletedAt,
        });
    }
}

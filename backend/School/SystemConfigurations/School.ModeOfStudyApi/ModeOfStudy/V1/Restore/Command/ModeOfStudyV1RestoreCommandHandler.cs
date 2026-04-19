namespace School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1RestoreCommandHandler(IModeOfStudyRepository repository)
    : ICommandHandler<ModeOfStudyV1RestoreCommand, ModeOfStudyV1RestoreCommandResult,
        SharedLibrary.Basics.Opaque.Domains.ModeOfStudy, IModeOfStudyRepository>
{
    public async Task<SuccessOrFailure<ModeOfStudyV1RestoreCommandResult>> HandleAsync(
        ModeOfStudyV1RestoreCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.ModeOfStudy>()
            .AddWhere(x => x.ModeOfStudyId == command.ModeOfStudyId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ModeOfStudyV1RestoreCommandResult>.EntityNotFound(typeof(ModeOfStudyV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<ModeOfStudyV1RestoreCommandResult>(
            new ModeOfStudyV1RestoreCommandResult { ModeOfStudyId = entity.ModeOfStudyId });
    }
}

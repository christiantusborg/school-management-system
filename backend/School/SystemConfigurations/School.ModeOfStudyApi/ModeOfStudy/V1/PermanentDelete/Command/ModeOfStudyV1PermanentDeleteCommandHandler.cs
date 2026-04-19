namespace School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1PermanentDeleteCommandHandler(IModeOfStudyRepository repository)
    : ICommandHandler<ModeOfStudyV1PermanentDeleteCommand, ModeOfStudyV1PermanentDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.ModeOfStudy, IModeOfStudyRepository>
{
    public async Task<SuccessOrFailure<ModeOfStudyV1PermanentDeleteCommandResult>> HandleAsync(
        ModeOfStudyV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.ModeOfStudy>()
            .AddWhere(x => x.ModeOfStudyId == command.ModeOfStudyId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ModeOfStudyV1PermanentDeleteCommandResult>.EntityNotFound(typeof(ModeOfStudyV1PermanentDeleteCommand));

        var id = entity.ModeOfStudyId;
        repository.Remove(entity);

        return new SuccessOrFailure<ModeOfStudyV1PermanentDeleteCommandResult>(
            new ModeOfStudyV1PermanentDeleteCommandResult { ModeOfStudyId = id });
    }
}

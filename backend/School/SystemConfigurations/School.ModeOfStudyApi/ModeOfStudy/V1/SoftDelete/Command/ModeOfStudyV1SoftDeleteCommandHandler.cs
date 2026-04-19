namespace School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1SoftDeleteCommandHandler(IModeOfStudyRepository repository)
    : ICommandHandler<ModeOfStudyV1SoftDeleteCommand, ModeOfStudyV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.ModeOfStudy, IModeOfStudyRepository>
{
    public async Task<SuccessOrFailure<ModeOfStudyV1SoftDeleteCommandResult>> HandleAsync(
        ModeOfStudyV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.ModeOfStudy>()
            .AddWhere(x => x.ModeOfStudyId == command.ModeOfStudyId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ModeOfStudyV1SoftDeleteCommandResult>.EntityNotFound(typeof(ModeOfStudyV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<ModeOfStudyV1SoftDeleteCommandResult>(
            new ModeOfStudyV1SoftDeleteCommandResult { ModeOfStudyId = entity.ModeOfStudyId });
    }
}

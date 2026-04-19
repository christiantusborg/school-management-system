namespace School.ModeOfStudyApi.ModeOfStudy.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1UpdateCommandHandler(IModeOfStudyRepository repository)
    : ICommandHandler<ModeOfStudyV1UpdateCommand, ModeOfStudyV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.ModeOfStudy, IModeOfStudyRepository>
{
    public async Task<SuccessOrFailure<ModeOfStudyV1UpdateCommandResult>> HandleAsync(
        ModeOfStudyV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.ModeOfStudy>()
            .AddWhere(x => x.ModeOfStudyId == command.ModeOfStudyId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ModeOfStudyV1UpdateCommandResult>.EntityNotFound(typeof(ModeOfStudyV1UpdateCommand));

        entity.Name = command.Name;


        return new SuccessOrFailure<ModeOfStudyV1UpdateCommandResult>(
            new ModeOfStudyV1UpdateCommandResult { ModeOfStudyId = entity.ModeOfStudyId });
    }
}

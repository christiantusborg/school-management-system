namespace School.ModeOfStudyApi.ModeOfStudy.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1GetCommandHandler(IModeOfStudyRepository repository)
    : ICommandHandler<ModeOfStudyV1GetCommand, ModeOfStudyV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.ModeOfStudy, IModeOfStudyRepository>
{
    public async Task<SuccessOrFailure<ModeOfStudyV1GetCommandResult>> HandleAsync(
        ModeOfStudyV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.ModeOfStudy>()
            .AddWhere(x => x.ModeOfStudyId == command.ModeOfStudyId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ModeOfStudyV1GetCommandResult>.EntityNotFound(typeof(ModeOfStudyV1GetCommand));

        return new SuccessOrFailure<ModeOfStudyV1GetCommandResult>(new ModeOfStudyV1GetCommandResult
        {
            ModeOfStudyId = entity.ModeOfStudyId,
            Name = entity.Name,

            DeletedAt = entity.DeletedAt
        });
    }
}

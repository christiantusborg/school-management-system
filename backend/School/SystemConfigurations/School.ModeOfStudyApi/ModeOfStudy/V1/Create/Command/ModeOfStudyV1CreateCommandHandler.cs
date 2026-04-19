namespace School.ModeOfStudyApi.ModeOfStudy.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ModeOfStudyV1CreateCommandHandler(IModeOfStudyRepository repository)
    : ICommandHandler<ModeOfStudyV1CreateCommand, ModeOfStudyV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.ModeOfStudy, IModeOfStudyRepository>
{
    public async Task<SuccessOrFailure<ModeOfStudyV1CreateCommandResult>> HandleAsync(
        ModeOfStudyV1CreateCommand command, CancellationToken cancellationToken)
    {
        var entity = new SharedLibrary.Basics.Opaque.Domains.ModeOfStudy
        {
            Name = command.Name,

        };
        repository.Add(entity);
        return new SuccessOrFailure<ModeOfStudyV1CreateCommandResult>(
            new ModeOfStudyV1CreateCommandResult { ModeOfStudyId = entity.ModeOfStudyId });
    }
}

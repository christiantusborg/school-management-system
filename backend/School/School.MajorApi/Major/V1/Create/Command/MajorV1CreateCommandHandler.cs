namespace School.MajorApi.Major.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MajorV1CreateCommandHandler(IMajorRepository repository)
    : ICommandHandler<MajorV1CreateCommand, MajorV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Major, IMajorRepository>
{
    public async Task<SuccessOrFailure<MajorV1CreateCommandResult>> HandleAsync(
        MajorV1CreateCommand command, CancellationToken cancellationToken)
    {
        var entity = new SharedLibrary.Basics.Opaque.Domains.Major
        {
            ProgrammeId = command.ProgrammeId,
            Name = command.Name,
        };
        repository.Add(entity);
        return new SuccessOrFailure<MajorV1CreateCommandResult>(
            new MajorV1CreateCommandResult { MajorId = entity.MajorId });
    }
}

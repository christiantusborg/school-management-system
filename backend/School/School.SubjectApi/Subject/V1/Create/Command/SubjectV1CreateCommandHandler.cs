namespace School.SubjectApi.Subject.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1CreateCommandHandler(ISubjectRepository repository)
    : ICommandHandler<SubjectV1CreateCommand, SubjectV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Subject, ISubjectRepository>
{
    public async Task<SuccessOrFailure<SubjectV1CreateCommandResult>> HandleAsync(
        SubjectV1CreateCommand command, CancellationToken cancellationToken)
    {
        var entity = new SharedLibrary.Basics.Opaque.Domains.Subject
        {
            MajorId = command.MajorId,
            Code = command.Code,
            Name = command.Name,
            Ects = command.Ects,
        };
        repository.Add(entity);
        return new SuccessOrFailure<SubjectV1CreateCommandResult>(
            new SubjectV1CreateCommandResult { SubjectId = entity.SubjectId });
    }
}

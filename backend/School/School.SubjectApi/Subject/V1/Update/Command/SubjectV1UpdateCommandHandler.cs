namespace School.SubjectApi.Subject.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1UpdateCommandHandler(ISubjectRepository repository)
    : ICommandHandler<SubjectV1UpdateCommand, SubjectV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Subject, ISubjectRepository>
{
    public async Task<SuccessOrFailure<SubjectV1UpdateCommandResult>> HandleAsync(
        SubjectV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Subject>()
            .AddWhere(x => x.SubjectId == command.SubjectId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<SubjectV1UpdateCommandResult>.EntityNotFound(typeof(SubjectV1UpdateCommand));

        entity.MajorId = command.MajorId;
        entity.Code = command.Code;
        entity.Name = command.Name;
        entity.Ects = command.Ects;

        return new SuccessOrFailure<SubjectV1UpdateCommandResult>(
            new SubjectV1UpdateCommandResult { SubjectId = entity.SubjectId });
    }
}

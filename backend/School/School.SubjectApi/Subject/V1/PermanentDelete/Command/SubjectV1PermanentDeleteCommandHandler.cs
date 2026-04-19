namespace School.SubjectApi.Subject.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1PermanentDeleteCommandHandler(ISubjectRepository repository)
    : ICommandHandler<SubjectV1PermanentDeleteCommand, SubjectV1PermanentDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Subject, ISubjectRepository>
{
    public async Task<SuccessOrFailure<SubjectV1PermanentDeleteCommandResult>> HandleAsync(
        SubjectV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Subject>()
            .AddWhere(x => x.SubjectId == command.SubjectId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<SubjectV1PermanentDeleteCommandResult>.EntityNotFound(typeof(SubjectV1PermanentDeleteCommand));

        var savedId = entity.SubjectId;
        repository.Remove(entity);

        return new SuccessOrFailure<SubjectV1PermanentDeleteCommandResult>(
            new SubjectV1PermanentDeleteCommandResult { SubjectId = savedId });
    }
}

namespace School.SubjectApi.Subject.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1RestoreCommandHandler(ISubjectRepository repository)
    : ICommandHandler<SubjectV1RestoreCommand, SubjectV1RestoreCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Subject, ISubjectRepository>
{
    public async Task<SuccessOrFailure<SubjectV1RestoreCommandResult>> HandleAsync(
        SubjectV1RestoreCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Subject>()
            .AddWhere(x => x.SubjectId == command.SubjectId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<SubjectV1RestoreCommandResult>.EntityNotFound(typeof(SubjectV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<SubjectV1RestoreCommandResult>(
            new SubjectV1RestoreCommandResult { SubjectId = entity.SubjectId });
    }
}

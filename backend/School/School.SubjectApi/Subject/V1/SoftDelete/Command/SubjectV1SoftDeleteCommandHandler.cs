namespace School.SubjectApi.Subject.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1SoftDeleteCommandHandler(ISubjectRepository repository)
    : ICommandHandler<SubjectV1SoftDeleteCommand, SubjectV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Subject, ISubjectRepository>
{
    public async Task<SuccessOrFailure<SubjectV1SoftDeleteCommandResult>> HandleAsync(
        SubjectV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Subject>()
            .AddWhere(x => x.SubjectId == command.SubjectId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<SubjectV1SoftDeleteCommandResult>.EntityNotFound(typeof(SubjectV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<SubjectV1SoftDeleteCommandResult>(
            new SubjectV1SoftDeleteCommandResult { SubjectId = entity.SubjectId });
    }
}

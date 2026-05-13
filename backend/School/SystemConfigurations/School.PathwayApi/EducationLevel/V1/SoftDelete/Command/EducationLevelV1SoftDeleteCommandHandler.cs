namespace School.PathwayApi.EducationLevel.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EducationLevelV1SoftDeleteCommandHandler(IEducationLevelRepository repository)
    : ICommandHandler<EducationLevelV1SoftDeleteCommand, EducationLevelV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.EducationLevel, IEducationLevelRepository>
{
    public async Task<SuccessOrFailure<EducationLevelV1SoftDeleteCommandResult>> HandleAsync(
        EducationLevelV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.EducationLevel>()
            .AddWhere(x => x.EducationLevelId == command.EducationLevelId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return SuccessOrFailureHelper<EducationLevelV1SoftDeleteCommandResult>.EntityNotFound(typeof(EducationLevelV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<EducationLevelV1SoftDeleteCommandResult>(
            new EducationLevelV1SoftDeleteCommandResult { EducationLevelId = entity.EducationLevelId });
    }
}

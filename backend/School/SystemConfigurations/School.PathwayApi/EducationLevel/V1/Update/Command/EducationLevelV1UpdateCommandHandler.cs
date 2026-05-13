namespace School.PathwayApi.EducationLevel.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EducationLevelV1UpdateCommandHandler(IEducationLevelRepository repository)
    : ICommandHandler<EducationLevelV1UpdateCommand, EducationLevelV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.EducationLevel, IEducationLevelRepository>
{
    public async Task<SuccessOrFailure<EducationLevelV1UpdateCommandResult>> HandleAsync(
        EducationLevelV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.EducationLevel>()
            .AddWhere(x => x.EducationLevelId == command.EducationLevelId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return SuccessOrFailureHelper<EducationLevelV1UpdateCommandResult>.EntityNotFound(typeof(EducationLevelV1UpdateCommand));

        entity.Name = command.Name;
        entity.Rank = command.Rank;
        entity.DisplayOrder = command.DisplayOrder;

        return new SuccessOrFailure<EducationLevelV1UpdateCommandResult>(
            new EducationLevelV1UpdateCommandResult { EducationLevelId = entity.EducationLevelId });
    }
}

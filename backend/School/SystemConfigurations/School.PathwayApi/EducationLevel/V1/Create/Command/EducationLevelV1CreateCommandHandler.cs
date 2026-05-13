namespace School.PathwayApi.EducationLevel.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EducationLevelV1CreateCommandHandler(IEducationLevelRepository repository)
    : ICommandHandler<EducationLevelV1CreateCommand, EducationLevelV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.EducationLevel, IEducationLevelRepository>
{
    public Task<SuccessOrFailure<EducationLevelV1CreateCommandResult>> HandleAsync(
        EducationLevelV1CreateCommand command, CancellationToken cancellationToken)
    {
        var entity = new SharedLibrary.Basics.Opaque.Domains.EducationLevel
        {
            EducationLevelId = Guid.NewGuid(),
            Name = command.Name,
            Rank = command.Rank,
            DisplayOrder = command.DisplayOrder,
        };
        repository.Add(entity);
        return Task.FromResult(new SuccessOrFailure<EducationLevelV1CreateCommandResult>(
            new EducationLevelV1CreateCommandResult { EducationLevelId = entity.EducationLevelId }));
    }
}

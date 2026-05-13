namespace School.PathwayApi.Pathway.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1CreateCommandHandler(OdinDbContext db)
    : ICommandHandler<PathwayV1CreateCommand, PathwayV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public Task<SuccessOrFailure<PathwayV1CreateCommandResult>> HandleAsync(
        PathwayV1CreateCommand command, CancellationToken cancellationToken)
    {
        var entity = new SharedLibrary.Basics.Opaque.Domains.Pathway
        {
            PathwayId = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description ?? string.Empty,
            MinimumYearsWorkExperience = command.MinimumYearsWorkExperience,
        };
        db.Pathways.Add(entity);

        foreach (var docId in command.DocumentTypeIds.Distinct())
        {
            db.PathwayDocumentRequirements.Add(new PathwayDocumentRequirement
            {
                PathwayDocumentRequirementId = Guid.NewGuid(),
                PathwayId = entity.PathwayId,
                DocumentTypeId = docId,
            });
        }

        foreach (var levelId in command.AcceptedEducationLevelIds.Distinct())
        {
            db.PathwayAcceptedEducationLevels.Add(new PathwayAcceptedEducationLevel
            {
                PathwayId = entity.PathwayId,
                EducationLevelId = levelId,
            });
        }

        return Task.FromResult(new SuccessOrFailure<PathwayV1CreateCommandResult>(
            new PathwayV1CreateCommandResult { PathwayId = entity.PathwayId }));
    }
}

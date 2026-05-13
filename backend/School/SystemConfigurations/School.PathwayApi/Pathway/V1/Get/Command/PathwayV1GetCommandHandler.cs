namespace School.PathwayApi.Pathway.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1GetCommandHandler(OdinDbContext db)
    : ICommandHandler<PathwayV1GetCommand, PathwayV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<PathwayV1GetCommandResult>> HandleAsync(
        PathwayV1GetCommand command, CancellationToken cancellationToken)
    {
        var pathway = await db.Pathways
            .Where(x => x.PathwayId == command.PathwayId && x.DeletedAt == null)
            .Select(x => new
            {
                x.PathwayId,
                x.Name,
                x.Description,
                x.MinimumYearsWorkExperience,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (pathway is null)
            return SuccessOrFailureHelper<PathwayV1GetCommandResult>.EntityNotFound(typeof(PathwayV1GetCommand));

        var documentTypeIds = await db.PathwayDocumentRequirements
            .Where(r => r.PathwayId == command.PathwayId && r.DeletedAt == null)
            .Select(r => r.DocumentTypeId)
            .ToListAsync(cancellationToken);

        var acceptedEducationLevelIds = await db.PathwayAcceptedEducationLevels
            .Where(a => a.PathwayId == command.PathwayId)
            .Select(a => a.EducationLevelId)
            .ToListAsync(cancellationToken);

        return new SuccessOrFailure<PathwayV1GetCommandResult>(new PathwayV1GetCommandResult
        {
            PathwayId = pathway.PathwayId,
            Name = pathway.Name,
            Description = pathway.Description,
            MinimumYearsWorkExperience = pathway.MinimumYearsWorkExperience,
            DocumentTypeIds = documentTypeIds,
            AcceptedEducationLevelIds = acceptedEducationLevelIds,
        });
    }
}

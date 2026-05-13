namespace School.PathwayApi.Pathway.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1UpdateCommandHandler(OdinDbContext db)
    : ICommandHandler<PathwayV1UpdateCommand, PathwayV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<PathwayV1UpdateCommandResult>> HandleAsync(
        PathwayV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var pathway = await db.Pathways
            .FirstOrDefaultAsync(p => p.PathwayId == command.PathwayId && p.DeletedAt == null, cancellationToken);
        if (pathway is null)
            return SuccessOrFailureHelper<PathwayV1UpdateCommandResult>.EntityNotFound(typeof(PathwayV1UpdateCommand));

        pathway.Name = command.Name;
        pathway.Description = command.Description ?? string.Empty;
        pathway.MinimumYearsWorkExperience = command.MinimumYearsWorkExperience;

        // Replace document requirements: hard-delete existing rows then re-add
        // the requested set. Soft delete would multiply rows on re-edit and
        // collide with the unique index on (PathwayId, DocumentTypeId).
        var existingDocs = await db.PathwayDocumentRequirements
            .Where(r => r.PathwayId == pathway.PathwayId)
            .ToListAsync(cancellationToken);
        db.PathwayDocumentRequirements.RemoveRange(existingDocs);
        foreach (var docId in command.DocumentTypeIds.Distinct())
        {
            db.PathwayDocumentRequirements.Add(new PathwayDocumentRequirement
            {
                PathwayDocumentRequirementId = Guid.NewGuid(),
                PathwayId = pathway.PathwayId,
                DocumentTypeId = docId,
            });
        }

        // Replace accepted education levels.
        var existingLevels = await db.PathwayAcceptedEducationLevels
            .Where(a => a.PathwayId == pathway.PathwayId)
            .ToListAsync(cancellationToken);
        db.PathwayAcceptedEducationLevels.RemoveRange(existingLevels);
        foreach (var levelId in command.AcceptedEducationLevelIds.Distinct())
        {
            db.PathwayAcceptedEducationLevels.Add(new PathwayAcceptedEducationLevel
            {
                PathwayId = pathway.PathwayId,
                EducationLevelId = levelId,
            });
        }

        return new SuccessOrFailure<PathwayV1UpdateCommandResult>(
            new PathwayV1UpdateCommandResult { PathwayId = pathway.PathwayId });
    }
}

namespace School.PathwayApi.Pathway.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1CreateCommandHandler(IPathwayRepository repository, OdinDbContext db)
    : ICommandHandler<PathwayV1CreateCommand, PathwayV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<PathwayV1CreateCommandResult>> HandleAsync(
        PathwayV1CreateCommand command, CancellationToken cancellationToken)
    {
        var distinctIds = command.DocumentTypeIds.Where(id => id > 0).Distinct().ToList();
        if (distinctIds.Count > 0)
        {
            var existingCount = await db.DocumentTypes
                .CountAsync(dt => distinctIds.Contains(dt.DocumentTypeId) && dt.DeletedAt == null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existingCount != distinctIds.Count)
                return SuccessOrFailureHelper<PathwayV1CreateCommandResult>.Create("Invalid_DocumentTypeIds");
        }

        var nextId = (await db.Pathways.MaxAsync(p => (int?)p.PathwayId, cancellationToken)
                          .ConfigureAwait(false) ?? 0) + 1;

        var entity = new SharedLibrary.Basics.Opaque.Domains.Pathway
        {
            PathwayId = nextId,
            Name = command.Name,
        };
        repository.Add(entity);

        foreach (var documentTypeId in distinctIds)
        {
            db.PathwayDocumentRequirements.Add(new PathwayDocumentRequirement
            {
                PathwayId = nextId,
                DocumentTypeId = documentTypeId,
            });
        }

        return new SuccessOrFailure<PathwayV1CreateCommandResult>(
            new PathwayV1CreateCommandResult { PathwayId = entity.PathwayId });
    }
}

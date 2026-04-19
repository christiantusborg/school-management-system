namespace School.PathwayApi.Pathway.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1UpdateCommandHandler(IPathwayRepository repository, OdinDbContext db)
    : ICommandHandler<PathwayV1UpdateCommand, PathwayV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<PathwayV1UpdateCommandResult>> HandleAsync(
        PathwayV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Pathway>()
            .AddWhere(x => x.PathwayId == command.PathwayId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<PathwayV1UpdateCommandResult>.EntityNotFound(typeof(PathwayV1UpdateCommand));

        var distinctIds = command.DocumentTypeIds.Where(id => id > 0).Distinct().ToList();
        if (distinctIds.Count > 0)
        {
            var existingCount = await db.DocumentTypes
                .CountAsync(dt => distinctIds.Contains(dt.DocumentTypeId) && dt.DeletedAt == null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existingCount != distinctIds.Count)
                return SuccessOrFailureHelper<PathwayV1UpdateCommandResult>.Create("Invalid_DocumentTypeIds");
        }

        entity.Name = command.Name;

        var existing = await db.PathwayDocumentRequirements
            .Where(r => r.PathwayId == command.PathwayId && r.DeletedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        foreach (var row in existing.Where(r => !distinctIds.Contains(r.DocumentTypeId)))
        {
            row.DeletedAt = now;
        }

        var existingIds = existing.Where(r => r.DeletedAt == null).Select(r => r.DocumentTypeId).ToHashSet();
        foreach (var documentTypeId in distinctIds.Where(id => !existingIds.Contains(id)))
        {
            db.PathwayDocumentRequirements.Add(new PathwayDocumentRequirement
            {
                PathwayId = command.PathwayId,
                DocumentTypeId = documentTypeId,
            });
        }

        return new SuccessOrFailure<PathwayV1UpdateCommandResult>(
            new PathwayV1UpdateCommandResult { PathwayId = entity.PathwayId });
    }
}

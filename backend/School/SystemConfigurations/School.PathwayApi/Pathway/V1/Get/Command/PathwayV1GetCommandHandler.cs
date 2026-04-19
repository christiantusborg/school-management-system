namespace School.PathwayApi.Pathway.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1GetCommandHandler(IPathwayRepository repository, OdinDbContext db)
    : ICommandHandler<PathwayV1GetCommand, PathwayV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<PathwayV1GetCommandResult>> HandleAsync(
        PathwayV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Pathway>()
            .AddWhere(x => x.PathwayId == command.PathwayId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<PathwayV1GetCommandResult>.EntityNotFound(typeof(PathwayV1GetCommand));

        var documentTypeIds = await db.PathwayDocumentRequirements
            .Where(r => r.PathwayId == command.PathwayId && r.DeletedAt == null)
            .Select(r => r.DocumentTypeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new SuccessOrFailure<PathwayV1GetCommandResult>(new PathwayV1GetCommandResult
        {
            PathwayId = entity.PathwayId,
            Name = entity.Name,
            DocumentTypeIds = documentTypeIds,
            DeletedAt = entity.DeletedAt
        });
    }
}

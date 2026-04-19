namespace School.PathwayApi.Pathway.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1SoftDeleteCommandHandler(IPathwayRepository repository)
    : ICommandHandler<PathwayV1SoftDeleteCommand, PathwayV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<PathwayV1SoftDeleteCommandResult>> HandleAsync(
        PathwayV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Pathway>()
            .AddWhere(x => x.PathwayId == command.PathwayId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<PathwayV1SoftDeleteCommandResult>.EntityNotFound(typeof(PathwayV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<PathwayV1SoftDeleteCommandResult>(
            new PathwayV1SoftDeleteCommandResult { PathwayId = entity.PathwayId });
    }
}

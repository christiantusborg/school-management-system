namespace School.PathwayApi.Pathway.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1RestoreCommandHandler(IPathwayRepository repository)
    : ICommandHandler<PathwayV1RestoreCommand, PathwayV1RestoreCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<PathwayV1RestoreCommandResult>> HandleAsync(
        PathwayV1RestoreCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Pathway>()
            .AddWhere(x => x.PathwayId == command.PathwayId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<PathwayV1RestoreCommandResult>.EntityNotFound(typeof(PathwayV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<PathwayV1RestoreCommandResult>(
            new PathwayV1RestoreCommandResult { PathwayId = entity.PathwayId });
    }
}

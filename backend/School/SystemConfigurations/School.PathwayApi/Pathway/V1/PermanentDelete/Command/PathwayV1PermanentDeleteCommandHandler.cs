namespace School.PathwayApi.Pathway.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PathwayV1PermanentDeleteCommandHandler(IPathwayRepository repository)
    : ICommandHandler<PathwayV1PermanentDeleteCommand, PathwayV1PermanentDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Pathway, IPathwayRepository>
{
    public async Task<SuccessOrFailure<PathwayV1PermanentDeleteCommandResult>> HandleAsync(
        PathwayV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Pathway>()
            .AddWhere(x => x.PathwayId == command.PathwayId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<PathwayV1PermanentDeleteCommandResult>.EntityNotFound(typeof(PathwayV1PermanentDeleteCommand));

        var id = entity.PathwayId;
        repository.Remove(entity);

        return new SuccessOrFailure<PathwayV1PermanentDeleteCommandResult>(
            new PathwayV1PermanentDeleteCommandResult { PathwayId = id });
    }
}

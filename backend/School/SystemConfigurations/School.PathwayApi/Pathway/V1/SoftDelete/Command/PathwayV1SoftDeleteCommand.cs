namespace School.PathwayApi.Pathway.V1.SoftDelete.Command;

public sealed record PathwayV1SoftDeleteCommand : IHandleableCommand<
    PathwayV1SoftDeleteCommand,
    PathwayV1SoftDeleteCommandValidator,
    PathwayV1SoftDeleteCommandHandler,
    PathwayV1SoftDeleteCommandResult>
{
    public required int PathwayId { get; init; }
}

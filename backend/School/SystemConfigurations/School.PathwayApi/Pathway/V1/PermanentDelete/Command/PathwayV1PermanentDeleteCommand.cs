namespace School.PathwayApi.Pathway.V1.PermanentDelete.Command;

public sealed record PathwayV1PermanentDeleteCommand : IHandleableCommand<
    PathwayV1PermanentDeleteCommand,
    PathwayV1PermanentDeleteCommandValidator,
    PathwayV1PermanentDeleteCommandHandler,
    PathwayV1PermanentDeleteCommandResult>
{
    public required int PathwayId { get; init; }
}

namespace School.PathwayApi.Pathway.V1.Restore.Command;

public sealed record PathwayV1RestoreCommand : IHandleableCommand<
    PathwayV1RestoreCommand,
    PathwayV1RestoreCommandValidator,
    PathwayV1RestoreCommandHandler,
    PathwayV1RestoreCommandResult>
{
    public required int PathwayId { get; init; }
}

namespace School.PathwayApi.Pathway.V1.Update.Command;

public sealed record PathwayV1UpdateCommand : IHandleableCommand<
    PathwayV1UpdateCommand,
    PathwayV1UpdateCommandValidator,
    PathwayV1UpdateCommandHandler,
    PathwayV1UpdateCommandResult>
{
    public required int PathwayId { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<int> DocumentTypeIds { get; init; } = [];
}

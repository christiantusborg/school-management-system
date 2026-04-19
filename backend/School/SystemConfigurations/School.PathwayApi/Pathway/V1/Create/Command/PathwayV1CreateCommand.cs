namespace School.PathwayApi.Pathway.V1.Create.Command;

public sealed record PathwayV1CreateCommand : IHandleableCommand<
    PathwayV1CreateCommand,
    PathwayV1CreateCommandValidator,
    PathwayV1CreateCommandHandler,
    PathwayV1CreateCommandResult>
{
    public required string Name { get; init; }
    public IReadOnlyList<int> DocumentTypeIds { get; init; } = [];
}

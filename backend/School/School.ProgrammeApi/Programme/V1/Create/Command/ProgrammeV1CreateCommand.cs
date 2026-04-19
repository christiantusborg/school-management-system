namespace School.ProgrammeApi.Programme.V1.Create.Command;

public sealed record ProgrammeV1CreateCommand : IHandleableCommand<
    ProgrammeV1CreateCommand,
    ProgrammeV1CreateCommandValidator,
    ProgrammeV1CreateCommandHandler,
    ProgrammeV1CreateCommandResult>
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public IReadOnlyList<int> PathwayIds { get; init; } = [];
}

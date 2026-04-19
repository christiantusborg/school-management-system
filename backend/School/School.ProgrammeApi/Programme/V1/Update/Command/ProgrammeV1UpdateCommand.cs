namespace School.ProgrammeApi.Programme.V1.Update.Command;

public sealed record ProgrammeV1UpdateCommand : IHandleableCommand<
    ProgrammeV1UpdateCommand,
    ProgrammeV1UpdateCommandValidator,
    ProgrammeV1UpdateCommandHandler,
    ProgrammeV1UpdateCommandResult>
{
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public IReadOnlyList<int> PathwayIds { get; init; } = [];
}

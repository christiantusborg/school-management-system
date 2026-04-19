namespace School.ProgrammeApi.Programme.V1.PermanentDelete.Command;

public sealed record ProgrammeV1PermanentDeleteCommand : IHandleableCommand<
    ProgrammeV1PermanentDeleteCommand,
    ProgrammeV1PermanentDeleteCommandValidator,
    ProgrammeV1PermanentDeleteCommandHandler,
    ProgrammeV1PermanentDeleteCommandResult>
{
    public required Guid ProgrammeId { get; init; }
}

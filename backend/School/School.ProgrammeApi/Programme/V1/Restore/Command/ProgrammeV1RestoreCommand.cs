namespace School.ProgrammeApi.Programme.V1.Restore.Command;

public sealed record ProgrammeV1RestoreCommand : IHandleableCommand<
    ProgrammeV1RestoreCommand,
    ProgrammeV1RestoreCommandValidator,
    ProgrammeV1RestoreCommandHandler,
    ProgrammeV1RestoreCommandResult>
{
    public required Guid ProgrammeId { get; init; }
}

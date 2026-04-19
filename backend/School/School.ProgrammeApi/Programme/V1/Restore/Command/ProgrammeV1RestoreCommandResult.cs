namespace School.ProgrammeApi.Programme.V1.Restore.Command;

public sealed class ProgrammeV1RestoreCommandResult : IProgrammeV1RestoreCommandResultQueue
{
    public required Guid ProgrammeId { get; init; }
}

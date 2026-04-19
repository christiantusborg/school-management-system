namespace School.ProgrammeApi.Programme.V1.PermanentDelete.Command;

public sealed class ProgrammeV1PermanentDeleteCommandResult : IProgrammeV1PermanentDeleteCommandResultQueue
{
    public required Guid ProgrammeId { get; init; }
}

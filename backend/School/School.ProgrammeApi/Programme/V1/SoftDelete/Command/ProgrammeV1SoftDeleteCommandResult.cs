namespace School.ProgrammeApi.Programme.V1.SoftDelete.Command;

public sealed class ProgrammeV1SoftDeleteCommandResult : IProgrammeV1SoftDeleteCommandResultQueue
{
    public required Guid ProgrammeId { get; init; }
}

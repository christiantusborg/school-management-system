namespace School.ProgrammeApi.Programme.V1.Update.Command;

public sealed class ProgrammeV1UpdateCommandResult : IProgrammeV1UpdateCommandResultQueue
{
    public required Guid ProgrammeId { get; init; }
}

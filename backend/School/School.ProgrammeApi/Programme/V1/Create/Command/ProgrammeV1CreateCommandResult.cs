namespace School.ProgrammeApi.Programme.V1.Create.Command;

public sealed class ProgrammeV1CreateCommandResult : IProgrammeV1CreateCommandResultQueue
{
    public required Guid ProgrammeId { get; init; }
}

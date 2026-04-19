namespace School.MajorApi.Major.V1.PermanentDelete.Command;

public sealed class MajorV1PermanentDeleteCommandResult : IMajorV1PermanentDeleteCommandResultQueue
{
    public required Guid MajorId { get; init; }
}

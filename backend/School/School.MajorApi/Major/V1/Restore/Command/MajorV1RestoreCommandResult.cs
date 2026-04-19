namespace School.MajorApi.Major.V1.Restore.Command;

public sealed class MajorV1RestoreCommandResult : IMajorV1RestoreCommandResultQueue
{
    public required Guid MajorId { get; init; }
}

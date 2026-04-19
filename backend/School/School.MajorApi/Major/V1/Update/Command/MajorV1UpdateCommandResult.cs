namespace School.MajorApi.Major.V1.Update.Command;

public sealed class MajorV1UpdateCommandResult : IMajorV1UpdateCommandResultQueue
{
    public required Guid MajorId { get; init; }
}

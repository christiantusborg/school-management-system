namespace School.MajorApi.Major.V1.Create.Command;

public sealed class MajorV1CreateCommandResult : IMajorV1CreateCommandResultQueue
{
    public required Guid MajorId { get; init; }
}

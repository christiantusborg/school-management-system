namespace School.MajorApi.Major.V1.SoftDelete.Command;

public sealed class MajorV1SoftDeleteCommandResult : IMajorV1SoftDeleteCommandResultQueue
{
    public required Guid MajorId { get; init; }
}

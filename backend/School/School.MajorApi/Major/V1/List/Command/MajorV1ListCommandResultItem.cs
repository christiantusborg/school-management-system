namespace School.MajorApi.Major.V1.List.Command;

public sealed class MajorV1ListCommandResultItem : IMajorV1ListCommandResultItemQueue
{
    public required Guid MajorId { get; init; }
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
    public DateTime? DeletedAt { get; init; }
}

namespace School.SubjectApi.Subject.V1.PermanentDelete.Command;

public sealed class SubjectV1PermanentDeleteCommandResult : ISubjectV1PermanentDeleteCommandResultQueue
{
    public required Guid SubjectId { get; init; }
}

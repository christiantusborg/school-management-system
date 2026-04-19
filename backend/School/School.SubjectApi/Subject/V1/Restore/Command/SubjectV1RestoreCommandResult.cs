namespace School.SubjectApi.Subject.V1.Restore.Command;

public sealed class SubjectV1RestoreCommandResult : ISubjectV1RestoreCommandResultQueue
{
    public required Guid SubjectId { get; init; }
}

namespace School.SubjectApi.Subject.V1.SoftDelete.Command;

public sealed class SubjectV1SoftDeleteCommandResult : ISubjectV1SoftDeleteCommandResultQueue
{
    public required Guid SubjectId { get; init; }
}

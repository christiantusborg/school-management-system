namespace School.SubjectApi.Subject.V1.Update.Command;

public sealed class SubjectV1UpdateCommandResult : ISubjectV1UpdateCommandResultQueue
{
    public required Guid SubjectId { get; init; }
}

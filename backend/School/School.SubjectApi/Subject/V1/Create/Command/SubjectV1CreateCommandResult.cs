namespace School.SubjectApi.Subject.V1.Create.Command;

public sealed class SubjectV1CreateCommandResult : ISubjectV1CreateCommandResultQueue
{
    public required Guid SubjectId { get; init; }
}

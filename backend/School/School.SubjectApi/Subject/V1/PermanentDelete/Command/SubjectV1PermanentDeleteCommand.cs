namespace School.SubjectApi.Subject.V1.PermanentDelete.Command;

public sealed record SubjectV1PermanentDeleteCommand : IHandleableCommand<
    SubjectV1PermanentDeleteCommand,
    SubjectV1PermanentDeleteCommandValidator,
    SubjectV1PermanentDeleteCommandHandler,
    SubjectV1PermanentDeleteCommandResult>
{
    public required Guid SubjectId { get; init; }
}

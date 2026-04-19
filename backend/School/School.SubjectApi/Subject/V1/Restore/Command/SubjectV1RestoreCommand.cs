namespace School.SubjectApi.Subject.V1.Restore.Command;

public sealed record SubjectV1RestoreCommand : IHandleableCommand<
    SubjectV1RestoreCommand,
    SubjectV1RestoreCommandValidator,
    SubjectV1RestoreCommandHandler,
    SubjectV1RestoreCommandResult>
{
    public required Guid SubjectId { get; init; }
}

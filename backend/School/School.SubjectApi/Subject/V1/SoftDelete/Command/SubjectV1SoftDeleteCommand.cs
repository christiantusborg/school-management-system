namespace School.SubjectApi.Subject.V1.SoftDelete.Command;

public sealed record SubjectV1SoftDeleteCommand : IHandleableCommand<
    SubjectV1SoftDeleteCommand,
    SubjectV1SoftDeleteCommandValidator,
    SubjectV1SoftDeleteCommandHandler,
    SubjectV1SoftDeleteCommandResult>
{
    public required Guid SubjectId { get; init; }
}

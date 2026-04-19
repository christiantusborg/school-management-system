namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Command;

public sealed record EnrollmentStatusV1RestoreCommand : IHandleableCommand<
    EnrollmentStatusV1RestoreCommand,
    EnrollmentStatusV1RestoreCommandValidator,
    EnrollmentStatusV1RestoreCommandHandler,
    EnrollmentStatusV1RestoreCommandResult>
{
    public required int EnrollmentStatusId { get; init; }
}

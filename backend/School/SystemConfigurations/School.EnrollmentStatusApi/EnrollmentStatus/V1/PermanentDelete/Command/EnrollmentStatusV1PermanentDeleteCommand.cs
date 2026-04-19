namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Command;

public sealed record EnrollmentStatusV1PermanentDeleteCommand : IHandleableCommand<
    EnrollmentStatusV1PermanentDeleteCommand,
    EnrollmentStatusV1PermanentDeleteCommandValidator,
    EnrollmentStatusV1PermanentDeleteCommandHandler,
    EnrollmentStatusV1PermanentDeleteCommandResult>
{
    public required int EnrollmentStatusId { get; init; }
}

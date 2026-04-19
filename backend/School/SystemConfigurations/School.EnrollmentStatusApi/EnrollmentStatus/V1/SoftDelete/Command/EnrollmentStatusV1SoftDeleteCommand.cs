namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Command;

public sealed record EnrollmentStatusV1SoftDeleteCommand : IHandleableCommand<
    EnrollmentStatusV1SoftDeleteCommand,
    EnrollmentStatusV1SoftDeleteCommandValidator,
    EnrollmentStatusV1SoftDeleteCommandHandler,
    EnrollmentStatusV1SoftDeleteCommandResult>
{
    public required int EnrollmentStatusId { get; init; }
}

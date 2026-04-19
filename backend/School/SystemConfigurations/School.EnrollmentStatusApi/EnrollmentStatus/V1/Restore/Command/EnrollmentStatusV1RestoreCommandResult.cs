namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Command;

public sealed class EnrollmentStatusV1RestoreCommandResult : IEnrollmentStatusV1RestoreCommandResultQueue
{
    public required int EnrollmentStatusId { get; init; }
}

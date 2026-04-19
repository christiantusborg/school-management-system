namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Command;

public sealed class EnrollmentStatusV1PermanentDeleteCommandResult : IEnrollmentStatusV1PermanentDeleteCommandResultQueue
{
    public required int EnrollmentStatusId { get; init; }
}

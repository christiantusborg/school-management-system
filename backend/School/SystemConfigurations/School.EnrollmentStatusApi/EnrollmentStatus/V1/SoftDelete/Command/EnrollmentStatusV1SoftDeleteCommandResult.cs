namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Command;

public sealed class EnrollmentStatusV1SoftDeleteCommandResult : IEnrollmentStatusV1SoftDeleteCommandResultQueue
{
    public required int EnrollmentStatusId { get; init; }
}

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Command;

public sealed class EnrollmentStatusV1UpdateCommandResult : IEnrollmentStatusV1UpdateCommandResultQueue
{
    public required int EnrollmentStatusId { get; init; }
}

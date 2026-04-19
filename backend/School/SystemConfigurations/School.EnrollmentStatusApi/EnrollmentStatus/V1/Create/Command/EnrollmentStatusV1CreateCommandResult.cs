namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Command;

public sealed class EnrollmentStatusV1CreateCommandResult : IEnrollmentStatusV1CreateCommandResultQueue
{
    public required int EnrollmentStatusId { get; init; }
}

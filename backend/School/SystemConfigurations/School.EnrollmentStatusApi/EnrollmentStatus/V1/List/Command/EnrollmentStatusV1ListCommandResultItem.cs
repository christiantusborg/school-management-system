namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Command;

public sealed class EnrollmentStatusV1ListCommandResultItem : IEnrollmentStatusV1ListCommandResultItemQueue
{
    public required int EnrollmentStatusId { get; init; }
    public required string Name { get; init; }
    public required bool AllowSetByPartner { get; init; }
}

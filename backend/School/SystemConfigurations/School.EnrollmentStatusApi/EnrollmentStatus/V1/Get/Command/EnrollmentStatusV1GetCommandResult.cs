namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Command;

public sealed class EnrollmentStatusV1GetCommandResult : IEnrollmentStatusV1GetCommandResultQueue
{
    public required int EnrollmentStatusId { get; init; }
    public required string Name { get; init; }
    public required bool AllowSetByPartner { get; init; }
    public DateTime? DeletedAt { get; init; }
}

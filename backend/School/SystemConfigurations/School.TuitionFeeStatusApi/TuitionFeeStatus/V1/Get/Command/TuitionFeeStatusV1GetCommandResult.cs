namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Command;

public sealed class TuitionFeeStatusV1GetCommandResult : ITuitionFeeStatusV1GetCommandResultQueue
{
    public required int TuitionFeeStatusId { get; init; }
    public required string Name { get; init; }
    public DateTime? DeletedAt { get; init; }
}

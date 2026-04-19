namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Command;

public sealed class TuitionFeeStatusV1SoftDeleteCommandResult : ITuitionFeeStatusV1SoftDeleteCommandResultQueue
{
    public required int TuitionFeeStatusId { get; init; }
}

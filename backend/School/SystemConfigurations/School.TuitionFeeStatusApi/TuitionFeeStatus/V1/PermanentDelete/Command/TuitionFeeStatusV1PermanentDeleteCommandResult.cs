namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Command;

public sealed class TuitionFeeStatusV1PermanentDeleteCommandResult : ITuitionFeeStatusV1PermanentDeleteCommandResultQueue
{
    public required int TuitionFeeStatusId { get; init; }
}

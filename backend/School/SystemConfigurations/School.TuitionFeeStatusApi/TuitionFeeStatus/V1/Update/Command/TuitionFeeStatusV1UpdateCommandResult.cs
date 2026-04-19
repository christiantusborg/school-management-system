namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Command;

public sealed class TuitionFeeStatusV1UpdateCommandResult : ITuitionFeeStatusV1UpdateCommandResultQueue
{
    public required int TuitionFeeStatusId { get; init; }
}

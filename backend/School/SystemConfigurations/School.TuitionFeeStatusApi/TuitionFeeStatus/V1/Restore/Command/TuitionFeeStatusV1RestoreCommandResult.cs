namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Command;

public sealed class TuitionFeeStatusV1RestoreCommandResult : ITuitionFeeStatusV1RestoreCommandResultQueue
{
    public required int TuitionFeeStatusId { get; init; }
}

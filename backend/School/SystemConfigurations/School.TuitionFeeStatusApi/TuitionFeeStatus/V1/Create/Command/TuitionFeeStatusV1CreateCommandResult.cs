namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Command;

public sealed class TuitionFeeStatusV1CreateCommandResult : ITuitionFeeStatusV1CreateCommandResultQueue
{
    public required int TuitionFeeStatusId { get; init; }
}

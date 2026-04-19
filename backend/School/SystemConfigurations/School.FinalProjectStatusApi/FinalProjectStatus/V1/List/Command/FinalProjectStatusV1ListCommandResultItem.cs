namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Command;

public sealed class FinalProjectStatusV1ListCommandResultItem : IFinalProjectStatusV1ListCommandResultItemQueue
{
    public required int FinalProjectStatusId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool AllowSetByPartner { get; init; }
}

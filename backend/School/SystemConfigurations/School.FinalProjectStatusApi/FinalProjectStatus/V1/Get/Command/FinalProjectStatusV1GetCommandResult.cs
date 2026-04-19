namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Command;

public sealed class FinalProjectStatusV1GetCommandResult : IFinalProjectStatusV1GetCommandResultQueue
{
    public required int FinalProjectStatusId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool AllowSetByPartner { get; init; }
    public DateTime? DeletedAt { get; init; }
}

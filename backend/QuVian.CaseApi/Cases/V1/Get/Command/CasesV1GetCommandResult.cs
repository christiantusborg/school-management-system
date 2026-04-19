namespace QuVian.CaseApi.Cases.V1.Get.Command;

public sealed class CasesV1GetCommandResult : ICasesV1GetCommandResultQueue
{
    public required Guid CaseId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required CaseStatus Status { get; init; }
    public required CasePriority Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public required string CreatedByUserId { get; init; }
    public DateTime? DeletedAt { get; init; }
}

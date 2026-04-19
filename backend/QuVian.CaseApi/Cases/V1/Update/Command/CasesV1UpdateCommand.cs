namespace QuVian.CaseApi.Cases.V1.Update.Command;

public sealed record CasesV1UpdateCommand : IHandleableCommand<
    CasesV1UpdateCommand,
    CasesV1UpdateCommandValidator,
    CasesV1UpdateCommandHandler,
    CasesV1UpdateCommandResult>
{
    public required Guid CaseId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public CaseStatus Status { get; init; }
    public CasePriority Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}

namespace QuVian.CaseApi.Cases.V1.List.Command;

public sealed record CasesV1ListCommand : IHandleableCommand<
    CasesV1ListCommand,
    CasesV1ListCommandValidator,
    CasesV1ListCommandHandler,
    CommandSearchResult<CasesV1ListCommandResultItem>>
{
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
    public CaseStatus? Status { get; init; }
    public CasePriority? Priority { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

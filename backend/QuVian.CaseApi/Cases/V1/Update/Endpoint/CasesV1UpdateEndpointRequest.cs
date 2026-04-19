namespace QuVian.CaseApi.Cases.V1.Update.Endpoint;

public class CasesV1UpdateEndpointRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public CaseStatus Status { get; init; }
    public CasePriority Priority { get; init; }
    public DateTime? DueDate { get; init; }
}

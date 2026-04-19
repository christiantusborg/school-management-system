namespace QuVian.CaseApi.Cases.V1.List.Endpoint;

public class CasesV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required Guid CaseId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required CaseStatus Status { get; init; }
    public required CasePriority Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public required string CreatedByUserId { get; init; }
}

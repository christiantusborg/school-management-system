namespace QuVian.CaseApi.Cases.V1.PermanentDelete.Endpoint;

public class CasesV1PermanentDeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseId { get; init; }
}

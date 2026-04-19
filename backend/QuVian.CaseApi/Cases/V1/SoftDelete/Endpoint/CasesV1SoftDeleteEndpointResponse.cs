namespace QuVian.CaseApi.Cases.V1.SoftDelete.Endpoint;

public class CasesV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseId { get; init; }
}

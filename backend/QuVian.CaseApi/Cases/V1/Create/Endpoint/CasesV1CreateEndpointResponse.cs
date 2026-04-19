namespace QuVian.CaseApi.Cases.V1.Create.Endpoint;

public class CasesV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseId { get; init; }
}

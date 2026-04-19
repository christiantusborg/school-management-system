namespace QuVian.CaseApi.Cases.V1.Update.Endpoint;

public class CasesV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseId { get; init; }
}

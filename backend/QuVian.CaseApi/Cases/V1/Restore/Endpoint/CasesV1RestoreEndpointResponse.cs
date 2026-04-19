namespace QuVian.CaseApi.Cases.V1.Restore.Endpoint;

public class CasesV1RestoreEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseId { get; init; }
}

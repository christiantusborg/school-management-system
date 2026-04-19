using QuVian.CaseApi.Cases.V1.Restore.Command;

namespace QuVian.CaseApi.Cases.V1.Restore.Endpoint.Mappers;

public class CasesV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CasesV1RestoreCommandResult, CasesV1RestoreEndpointResponse>
{
    public CasesV1RestoreEndpointResponse MapFrom(CasesV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CasesV1RestoreEndpointResponse
        {
            CaseId = input.CaseId,
            Links = HateoasLinksHelper.AsUnDelete(httpContextAccessor, input.CaseId)
        };
    }
}

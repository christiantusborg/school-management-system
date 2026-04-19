using QuVian.CaseApi.Cases.V1.Update.Command;

namespace QuVian.CaseApi.Cases.V1.Update.Endpoint.Mappers;

public class CasesV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CasesV1UpdateCommandResult, CasesV1UpdateEndpointResponse>
{
    public CasesV1UpdateEndpointResponse MapFrom(CasesV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CasesV1UpdateEndpointResponse
        {
            CaseId = input.CaseId,
            Links = HateoasLinksHelper.AsUpdate(httpContextAccessor, input.CaseId)
        };
    }
}

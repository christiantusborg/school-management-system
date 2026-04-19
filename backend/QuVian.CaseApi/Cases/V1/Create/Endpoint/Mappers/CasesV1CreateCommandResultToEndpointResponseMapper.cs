using QuVian.CaseApi.Cases.V1.Create.Command;

namespace QuVian.CaseApi.Cases.V1.Create.Endpoint.Mappers;

public class CasesV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CasesV1CreateCommandResult, CasesV1CreateEndpointResponse>
{
    public CasesV1CreateEndpointResponse MapFrom(CasesV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CasesV1CreateEndpointResponse
        {
            CaseId = input.CaseId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.CaseId)
        };
    }
}

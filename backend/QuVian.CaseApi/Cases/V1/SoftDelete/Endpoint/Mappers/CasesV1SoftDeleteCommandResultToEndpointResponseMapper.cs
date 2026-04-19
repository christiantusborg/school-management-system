using QuVian.CaseApi.Cases.V1.SoftDelete.Command;

namespace QuVian.CaseApi.Cases.V1.SoftDelete.Endpoint.Mappers;

public class CasesV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CasesV1SoftDeleteCommandResult, CasesV1SoftDeleteEndpointResponse>
{
    public CasesV1SoftDeleteEndpointResponse MapFrom(CasesV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CasesV1SoftDeleteEndpointResponse
        {
            CaseId = input.CaseId,
            Links = HateoasLinksHelper.AsDelete(httpContextAccessor, input.CaseId)
        };
    }
}

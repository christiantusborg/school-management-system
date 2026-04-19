using QuVian.CaseApi.Cases.V1.PermanentDelete.Command;

namespace QuVian.CaseApi.Cases.V1.PermanentDelete.Endpoint.Mappers;

public class CasesV1PermanentDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CasesV1PermanentDeleteCommandResult, CasesV1PermanentDeleteEndpointResponse>
{
    public CasesV1PermanentDeleteEndpointResponse MapFrom(CasesV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CasesV1PermanentDeleteEndpointResponse
        {
            CaseId = input.CaseId,
            Links = HateoasLinksHelper.AsDelete(httpContextAccessor, input.CaseId)
        };
    }
}

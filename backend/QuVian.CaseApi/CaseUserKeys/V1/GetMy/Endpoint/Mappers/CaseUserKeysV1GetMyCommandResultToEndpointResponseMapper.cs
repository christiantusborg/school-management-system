using QuVian.CaseApi.CaseUserKeys.V1.GetMy.Command;

namespace QuVian.CaseApi.CaseUserKeys.V1.GetMy.Endpoint.Mappers;

public class CaseUserKeysV1GetMyCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseUserKeysV1GetMyCommandResult, CaseUserKeysV1GetMyEndpointResponse>
{
    public CaseUserKeysV1GetMyEndpointResponse MapFrom(CaseUserKeysV1GetMyCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseUserKeysV1GetMyEndpointResponse { Keys = input.Keys, Links = [] };
    }
}

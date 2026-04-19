using QuVian.CaseApi.CaseKeyPairs.V1.List.Command;

namespace QuVian.CaseApi.CaseKeyPairs.V1.List.Endpoint.Mappers;

public class CaseKeyPairsV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseKeyPairsV1ListCommandResult, CaseKeyPairsV1ListEndpointResponse>
{
    public CaseKeyPairsV1ListEndpointResponse MapFrom(CaseKeyPairsV1ListCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseKeyPairsV1ListEndpointResponse
        {
            Items          = input.Items,
            LabelOverrides = input.LabelOverrides,
            Links          = []
        };
    }
}

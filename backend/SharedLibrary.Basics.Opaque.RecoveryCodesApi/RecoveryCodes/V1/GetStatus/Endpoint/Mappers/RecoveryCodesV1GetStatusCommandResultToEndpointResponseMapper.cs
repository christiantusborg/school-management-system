using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Endpoint.Mappers;

public class RecoveryCodesV1GetStatusCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<RecoveryCodesV1GetStatusCommandResult, RecoveryCodesV1GetStatusEndpointResponse>
{
    public RecoveryCodesV1GetStatusEndpointResponse MapFrom(RecoveryCodesV1GetStatusCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new RecoveryCodesV1GetStatusEndpointResponse
        {
            RemainingCount = input.RemainingCount,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}

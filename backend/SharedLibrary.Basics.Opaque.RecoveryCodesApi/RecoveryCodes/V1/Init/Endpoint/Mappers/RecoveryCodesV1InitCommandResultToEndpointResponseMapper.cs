using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Endpoint.Mappers;

public class RecoveryCodesV1InitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<RecoveryCodesV1InitCommandResult, RecoveryCodesV1InitEndpointResponse>
{
    public RecoveryCodesV1InitEndpointResponse MapFrom(RecoveryCodesV1InitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new RecoveryCodesV1InitEndpointResponse
        {
            BatchId = input.BatchId,
            EvaluatedElements = input.EvaluatedElements,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.BatchId)
        };
    }
}

using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Endpoint.Mappers;

public class RecoveryCodesV1FinalizeCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<RecoveryCodesV1FinalizeCommandResult, RecoveryCodesV1FinalizeEndpointResponse>
{
    public RecoveryCodesV1FinalizeEndpointResponse MapFrom(RecoveryCodesV1FinalizeCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new RecoveryCodesV1FinalizeEndpointResponse
        {
            BatchId = input.BatchId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.BatchId)
        };
    }
}

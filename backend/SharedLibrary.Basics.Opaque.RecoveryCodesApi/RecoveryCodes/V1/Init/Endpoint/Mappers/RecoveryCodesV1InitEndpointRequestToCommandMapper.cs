using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Endpoint.Mappers;

public class RecoveryCodesV1InitEndpointRequestToCommandMapper
    : IMapper<RecoveryCodesV1InitEndpointRequest, RecoveryCodesV1InitCommand>
{
    public RecoveryCodesV1InitCommand MapFrom(RecoveryCodesV1InitEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new RecoveryCodesV1InitCommand
        {
            UserId = string.Empty, // overwritten in endpoint
            CodeIds = input.CodeIds,
            BlindedElements = input.BlindedElements,
        };
    }
}

using SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Endpoint.Mappers;

public sealed class RecoveryLoginV1InitEndpointRequestToCommandMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<RecoveryLoginV1InitEndpointRequest, RecoveryLoginV1InitCommand>
{
    public RecoveryLoginV1InitCommand MapFrom(RecoveryLoginV1InitEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new RecoveryLoginV1InitCommand
        {
            Username = input.Username,
            CodeId = input.CodeId,
            BlindedElement = input.BlindedElement,
            DeviceInfo = input.DeviceInfo
        };
    }
}

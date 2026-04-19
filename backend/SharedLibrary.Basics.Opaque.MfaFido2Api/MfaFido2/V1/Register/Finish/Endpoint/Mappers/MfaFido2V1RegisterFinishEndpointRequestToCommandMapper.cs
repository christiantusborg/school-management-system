using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Endpoint.Mappers;

public class MfaFido2V1RegisterFinishEndpointRequestToCommandMapper
    : IMapper<MfaFido2V1RegisterFinishEndpointRequest, MfaFido2V1RegisterFinishCommand>
{
    public MfaFido2V1RegisterFinishCommand MapFrom(MfaFido2V1RegisterFinishEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaFido2V1RegisterFinishCommand
        {
            UserId = string.Empty,
            Label = input.Label,
            AttestationResponse = input.AttestationResponse
        };
    }
}

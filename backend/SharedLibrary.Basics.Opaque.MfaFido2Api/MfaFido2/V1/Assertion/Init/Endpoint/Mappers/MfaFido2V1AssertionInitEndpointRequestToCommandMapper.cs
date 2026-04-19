using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Endpoint.Mappers;

public class MfaFido2V1AssertionInitEndpointRequestToCommandMapper
    : IMapper<MfaFido2V1AssertionInitEndpointRequest, MfaFido2V1AssertionInitCommand>
{
    public MfaFido2V1AssertionInitCommand MapFrom(MfaFido2V1AssertionInitEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaFido2V1AssertionInitCommand
        {
            PendingId = input.PendingId
        };
    }
}

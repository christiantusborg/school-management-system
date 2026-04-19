using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Endpoint.Mappers;

public class MfaFido2V1AssertionFinishEndpointRequestToCommandMapper
    : IMapper<MfaFido2V1AssertionFinishEndpointRequest, MfaFido2V1AssertionFinishCommand>
{
    public MfaFido2V1AssertionFinishCommand MapFrom(MfaFido2V1AssertionFinishEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaFido2V1AssertionFinishCommand
        {
            PendingId = input.PendingId,
            AssertionResponse = input.AssertionResponse
        };
    }
}

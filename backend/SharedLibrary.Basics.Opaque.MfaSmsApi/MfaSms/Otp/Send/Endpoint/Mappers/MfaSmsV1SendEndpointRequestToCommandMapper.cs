using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Endpoint.Mappers;

public class MfaSmsV1SendEndpointRequestToCommandMapper
    : IMapper<MfaSmsV1SendEndpointRequest, MfaSmsV1SendCommand>
{
    public MfaSmsV1SendCommand MapFrom(MfaSmsV1SendEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaSmsV1SendCommand
        {
            PendingId = input.PendingId,
        };
    }
}

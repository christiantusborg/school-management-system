using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Endpoint.Mappers;

public class MfaEmailV1SendEndpointRequestToCommandMapper
    : IMapper<MfaEmailV1SendEndpointRequest, MfaEmailV1SendCommand>
{
    public MfaEmailV1SendCommand MapFrom(MfaEmailV1SendEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaEmailV1SendCommand
        {
            PendingId = input.PendingId,
        };
    }
}

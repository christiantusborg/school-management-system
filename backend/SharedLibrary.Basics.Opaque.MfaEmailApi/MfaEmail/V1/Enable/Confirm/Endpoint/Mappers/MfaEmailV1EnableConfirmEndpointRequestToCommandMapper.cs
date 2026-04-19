using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Endpoint.Mappers;

public class MfaEmailV1EnableConfirmEndpointRequestToCommandMapper
    : IMapper<MfaEmailV1EnableConfirmEndpointRequest, MfaEmailV1EnableConfirmCommand>
{
    public MfaEmailV1EnableConfirmCommand MapFrom(MfaEmailV1EnableConfirmEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaEmailV1EnableConfirmCommand
        {
            SessionId = input.SessionId,
            Code = input.Code,
        };
    }
}

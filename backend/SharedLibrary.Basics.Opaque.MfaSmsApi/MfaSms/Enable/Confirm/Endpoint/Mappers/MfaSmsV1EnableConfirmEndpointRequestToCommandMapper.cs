using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Endpoint.Mappers;

public class MfaSmsV1EnableConfirmEndpointRequestToCommandMapper
    : IMapper<MfaSmsV1EnableConfirmEndpointRequest, MfaSmsV1EnableConfirmCommand>
{
    public MfaSmsV1EnableConfirmCommand MapFrom(MfaSmsV1EnableConfirmEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaSmsV1EnableConfirmCommand
        {
            SessionId = input.SessionId,
            Code = input.Code,
        };
    }
}

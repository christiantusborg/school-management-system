using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Endpoint.Mappers;

public class MfaSmsV1VerifyEndpointRequestToCommandMapper
    : IMapper<MfaSmsV1VerifyEndpointRequest, MfaSmsV1VerifyCommand>
{
    public MfaSmsV1VerifyCommand MapFrom(MfaSmsV1VerifyEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaSmsV1VerifyCommand
        {
            PendingId = input.PendingId,
            Code = input.Code,
        };
    }
}

using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Command;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Endpoint.Mappers;

public class MfaTotpV1VerifyEndpointRequestToCommandMapper
    : IMapper<MfaTotpV1VerifyEndpointRequest, MfaTotpV1VerifyCommand>
{
    public MfaTotpV1VerifyCommand MapFrom(MfaTotpV1VerifyEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaTotpV1VerifyCommand
        {
            PendingId = input.PendingId,
            Code = input.Code
        };
    }
}

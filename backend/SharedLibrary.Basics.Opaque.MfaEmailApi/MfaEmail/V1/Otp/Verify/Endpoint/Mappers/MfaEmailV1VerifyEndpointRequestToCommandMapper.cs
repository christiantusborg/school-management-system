using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Endpoint.Mappers;

public class MfaEmailV1VerifyEndpointRequestToCommandMapper
    : IMapper<MfaEmailV1VerifyEndpointRequest, MfaEmailV1VerifyCommand>
{
    public MfaEmailV1VerifyCommand MapFrom(MfaEmailV1VerifyEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaEmailV1VerifyCommand
        {
            PendingId = input.PendingId,
            Code = input.Code,
        };
    }
}

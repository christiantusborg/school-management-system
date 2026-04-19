using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Endpoint.Mappers;

public class PhonesV1VerifyConfirmEndpointRequestToCommandMapper
    : IMapper<PhonesV1VerifyConfirmEndpointRequest, PhonesV1VerifyConfirmCommand>
{
    public PhonesV1VerifyConfirmCommand MapFrom(PhonesV1VerifyConfirmEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1VerifyConfirmCommand
        {
            SessionId = input.SessionId,
            Code = input.Code,
        };
    }
}

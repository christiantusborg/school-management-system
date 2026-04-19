using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Endpoint.Mappers;

public class ChangePasswordV1InitEndpointRequestToCommandMapper
    : IMapper<ChangePasswordV1InitEndpointRequest, ChangePasswordV1InitCommand>
{
    public ChangePasswordV1InitCommand MapFrom(ChangePasswordV1InitEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ChangePasswordV1InitCommand
        {
            UserId = string.Empty, // overwritten in endpoint
            OldBlindedElement = input.OldBlindedElement,
            BlindedElement = input.BlindedElement,
        };
    }
}

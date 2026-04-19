using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Endpoint.Mappers;

public class LoginV1InitEndpointRequestToCommandMapper
    : IMapper<LoginV1InitEndpointRequest, LoginV1InitCommand>
{
    public LoginV1InitCommand MapFrom(LoginV1InitEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new LoginV1InitCommand
        {
            Username = input.Username,
            BlindedElement = input.BlindedElement,
            DeviceInfo = input.DeviceInfo
        };
    }
}

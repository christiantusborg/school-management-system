using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Command;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Endpoint.Mappers;

public class LoginV1FinishEndpointRequestToCommandMapper
    : IMapper<LoginV1FinishEndpointRequest, LoginV1FinishCommand>
{
    public LoginV1FinishCommand MapFrom(LoginV1FinishEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new LoginV1FinishCommand
        {
            LoginId = input.LoginId,
            Signature = input.Signature
        };
    }
}

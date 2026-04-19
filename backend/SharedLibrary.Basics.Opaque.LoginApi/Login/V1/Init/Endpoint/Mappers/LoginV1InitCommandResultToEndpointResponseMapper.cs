using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Endpoint.Mappers;

public class LoginV1InitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<LoginV1InitCommandResult, LoginV1InitEndpointResponse>
{
    public LoginV1InitEndpointResponse MapFrom(LoginV1InitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new LoginV1InitEndpointResponse
        {
            LoginId = input.LoginId,
            EvaluatedElement = input.EvaluatedElement,
            Challenge = input.Challenge,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}

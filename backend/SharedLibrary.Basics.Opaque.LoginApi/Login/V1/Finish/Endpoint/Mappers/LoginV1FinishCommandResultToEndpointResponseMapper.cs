using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Command;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Endpoint.Mappers;

public class LoginV1FinishCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<LoginV1FinishCommandResult, LoginV1FinishEndpointResponse>
{
    public LoginV1FinishEndpointResponse MapFrom(LoginV1FinishCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new LoginV1FinishEndpointResponse
        {
            Token = input.Token,
            ExpiresAt = input.ExpiresAt,
            MfaPendingId = input.MfaPendingId,
            AvailableMethods = input.AvailableMethods,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}

using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Command;

namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Endpoint.Mappers;

public class LogoutV1LogoutCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<LogoutV1LogoutCommandResult, LogoutV1LogoutEndpointResponse>
{
    public LogoutV1LogoutEndpointResponse MapFrom(LogoutV1LogoutCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new LogoutV1LogoutEndpointResponse
        {
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}

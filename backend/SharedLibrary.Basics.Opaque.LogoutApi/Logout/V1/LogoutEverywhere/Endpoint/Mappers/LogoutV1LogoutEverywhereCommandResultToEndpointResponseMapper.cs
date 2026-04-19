using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Command;

namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Endpoint.Mappers;

public class LogoutV1LogoutEverywhereCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<LogoutV1LogoutEverywhereCommandResult, LogoutV1LogoutEverywhereEndpointResponse>
{
    public LogoutV1LogoutEverywhereEndpointResponse MapFrom(LogoutV1LogoutEverywhereCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new LogoutV1LogoutEverywhereEndpointResponse
        {
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}

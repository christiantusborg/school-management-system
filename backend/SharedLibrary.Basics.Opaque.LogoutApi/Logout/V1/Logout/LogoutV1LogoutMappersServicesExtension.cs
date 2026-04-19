using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Command;
using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Endpoint;
using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class LogoutV1LogoutMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<LogoutV1LogoutCommandResult, LogoutV1LogoutEndpointResponse>,
            LogoutV1LogoutCommandResultToEndpointResponseMapper>();
        return services;
    }
}

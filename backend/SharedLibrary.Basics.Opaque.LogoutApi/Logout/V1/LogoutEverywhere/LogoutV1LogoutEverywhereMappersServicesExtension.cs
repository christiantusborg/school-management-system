using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Command;
using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Endpoint;
using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class LogoutV1LogoutEverywhereMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<LogoutV1LogoutEverywhereCommandResult, LogoutV1LogoutEverywhereEndpointResponse>,
            LogoutV1LogoutEverywhereCommandResultToEndpointResponseMapper>();
        return services;
    }
}

using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Command;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Endpoint;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AdminUsersV1DisableMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AdminUsersV1DisableCommandResult, AdminUsersV1DisableEndpointResponse>,
            AdminUsersV1DisableCommandResultToEndpointResponseMapper>();
        return services;
    }
}

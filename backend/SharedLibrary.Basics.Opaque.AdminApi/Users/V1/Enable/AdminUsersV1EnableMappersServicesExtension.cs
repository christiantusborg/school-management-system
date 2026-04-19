using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Command;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Endpoint;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AdminUsersV1EnableMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AdminUsersV1EnableCommandResult, AdminUsersV1EnableEndpointResponse>,
            AdminUsersV1EnableCommandResultToEndpointResponseMapper>();
        return services;
    }
}

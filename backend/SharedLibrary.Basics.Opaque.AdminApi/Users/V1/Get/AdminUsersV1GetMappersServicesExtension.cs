using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Command;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Endpoint;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AdminUsersV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AdminUsersV1GetCommandResult, AdminUsersV1GetEndpointResponse>,
            AdminUsersV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}

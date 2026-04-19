using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Command;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Endpoint;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AdminUsersV1ChangeRoleMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AdminUsersV1ChangeRoleCommandResult, AdminUsersV1ChangeRoleEndpointResponse>,
            AdminUsersV1ChangeRoleCommandResultToEndpointResponseMapper>();
        return services;
    }
}

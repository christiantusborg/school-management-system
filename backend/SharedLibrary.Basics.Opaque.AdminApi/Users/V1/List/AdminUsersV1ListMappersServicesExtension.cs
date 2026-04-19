using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Command;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Endpoint;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AdminUsersV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<AdminUsersV1ListCommandResultItem>, AdminUsersV1ListEndpointResponse>,
            AdminUsersV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}

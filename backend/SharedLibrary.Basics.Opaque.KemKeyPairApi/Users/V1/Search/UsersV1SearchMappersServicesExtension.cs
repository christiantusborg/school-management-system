using SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Command;
using SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Endpoint;
using SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class UsersV1SearchMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<UsersV1SearchCommandResult, UsersV1SearchEndpointResponse>,
            UsersV1SearchCommandResultToEndpointResponseMapper>();
        return services;
    }
}

using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class AddressesV1DeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AddressesV1DeleteCommandResult, AddressesV1DeleteEndpointResponse>,
            AddressesV1DeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

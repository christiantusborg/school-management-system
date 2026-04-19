using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class AddressesV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AddressesV1CreateCommandResult, AddressesV1CreateEndpointResponse>,
            AddressesV1CreateCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<AddressesV1CreateEndpointRequest, AddressesV1CreateCommand>,
            AddressesV1CreateEndpointRequestToCommandMapper>();
        return services;
    }
}

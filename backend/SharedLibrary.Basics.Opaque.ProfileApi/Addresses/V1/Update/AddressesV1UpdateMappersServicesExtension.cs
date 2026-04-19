using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class AddressesV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AddressesV1UpdateCommandResult, AddressesV1UpdateEndpointResponse>,
            AddressesV1UpdateCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<AddressesV1UpdateEndpointRequest, AddressesV1UpdateCommand>,
            AddressesV1UpdateEndpointRequestToCommandMapper>();
        return services;
    }
}

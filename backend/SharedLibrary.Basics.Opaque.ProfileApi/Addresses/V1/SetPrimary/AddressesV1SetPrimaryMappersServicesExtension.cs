using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class AddressesV1SetPrimaryMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AddressesV1SetPrimaryCommandResult, AddressesV1SetPrimaryEndpointResponse>,
            AddressesV1SetPrimaryCommandResultToEndpointResponseMapper>();
        return services;
    }
}

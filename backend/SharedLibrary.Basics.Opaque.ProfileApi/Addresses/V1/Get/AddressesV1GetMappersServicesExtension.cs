using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class AddressesV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AddressesV1GetCommandResult, AddressesV1GetEndpointResponse>,
            AddressesV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}

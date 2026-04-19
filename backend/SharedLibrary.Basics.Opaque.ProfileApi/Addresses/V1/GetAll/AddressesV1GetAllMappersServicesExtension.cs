using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class AddressesV1GetAllMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CommandSearchResult<AddressesV1GetAllCommandResultItem>, BaseGetAllResponse<AddressesV1GetAllEndpointResponseItem>>,
            AddressesV1GetAllCommandResultToEndpointResponseMapper>();
        return services;
    }
}

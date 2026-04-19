using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class PhonesV1GetAllMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CommandSearchResult<PhonesV1GetAllCommandResultItem>, BaseGetAllResponse<PhonesV1GetAllEndpointResponseItem>>,
            PhonesV1GetAllCommandResultToEndpointResponseMapper>();
        return services;
    }
}

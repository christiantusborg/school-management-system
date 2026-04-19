using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class PhonesV1DeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PhonesV1DeleteCommandResult, PhonesV1DeleteEndpointResponse>,
            PhonesV1DeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class PhonesV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PhonesV1GetCommandResult, PhonesV1GetEndpointResponse>,
            PhonesV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}

using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ProfileV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ProfileV1GetCommandResult, ProfileV1GetEndpointResponse>,
            ProfileV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}

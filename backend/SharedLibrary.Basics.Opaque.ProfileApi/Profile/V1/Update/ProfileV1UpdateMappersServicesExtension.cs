using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ProfileV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ProfileV1UpdateCommandResult, ProfileV1UpdateEndpointResponse>,
            ProfileV1UpdateCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<ProfileV1UpdateEndpointRequest, ProfileV1UpdateCommand>,
            ProfileV1UpdateEndpointRequestToCommandMapper>();
        return services;
    }
}

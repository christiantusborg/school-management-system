using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class PhonesV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PhonesV1UpdateCommandResult, PhonesV1UpdateEndpointResponse>,
            PhonesV1UpdateCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<PhonesV1UpdateEndpointRequest, PhonesV1UpdateCommand>,
            PhonesV1UpdateEndpointRequestToCommandMapper>();
        return services;
    }
}

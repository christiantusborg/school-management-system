using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class PhonesV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PhonesV1CreateCommandResult, PhonesV1CreateEndpointResponse>,
            PhonesV1CreateCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<PhonesV1CreateEndpointRequest, PhonesV1CreateCommand>,
            PhonesV1CreateEndpointRequestToCommandMapper>();
        return services;
    }
}

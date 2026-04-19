using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Command;
using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Endpoint;
using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class KemKeyPairV1SaveMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<KemKeyPairV1SaveCommandResult, KemKeyPairV1SaveEndpointResponse>,
            KemKeyPairV1SaveCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<KemKeyPairV1SaveEndpointRequest, KemKeyPairV1SaveCommand>,
            KemKeyPairV1SaveEndpointRequestToCommandMapper>();
        return services;
    }
}

using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Command;
using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Endpoint;
using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class KemKeyPairV1GetPublicKeyMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<KemKeyPairV1GetPublicKeyCommandResult, KemKeyPairV1GetPublicKeyEndpointResponse>,
            KemKeyPairV1GetPublicKeyCommandResultToEndpointResponseMapper>();
        return services;
    }
}

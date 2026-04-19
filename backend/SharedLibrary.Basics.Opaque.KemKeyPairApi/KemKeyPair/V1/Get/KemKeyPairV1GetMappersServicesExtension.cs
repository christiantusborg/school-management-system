using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Command;
using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Endpoint;
using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class KemKeyPairV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<KemKeyPairV1GetCommandResult, KemKeyPairV1GetEndpointResponse>,
            KemKeyPairV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}

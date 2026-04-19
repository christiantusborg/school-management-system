using SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Command;
using SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Endpoint;
using SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class MeV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MeV1GetCommandResult, MeV1GetEndpointResponse>,
            MeV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}

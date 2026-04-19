using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Command;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Endpoint;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaSmsV1DeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaSmsV1DeleteCommandResult, MfaSmsV1DeleteEndpointResponse>,
            MfaSmsV1DeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

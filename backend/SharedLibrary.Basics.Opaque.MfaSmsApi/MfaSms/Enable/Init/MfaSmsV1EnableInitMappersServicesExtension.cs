using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Command;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Endpoint;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaSmsV1EnableInitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaSmsV1EnableInitCommandResult, MfaSmsV1EnableInitEndpointResponse>,
            MfaSmsV1EnableInitCommandResultToEndpointResponseMapper>();
        return services;
    }
}

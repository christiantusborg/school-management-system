using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Command;
using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Endpoint;
using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaTotpV1EnableInitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaTotpV1EnableInitCommandResult, MfaTotpV1EnableInitEndpointResponse>,
            MfaTotpV1EnableInitCommandResultToEndpointResponseMapper>();
        return services;
    }
}

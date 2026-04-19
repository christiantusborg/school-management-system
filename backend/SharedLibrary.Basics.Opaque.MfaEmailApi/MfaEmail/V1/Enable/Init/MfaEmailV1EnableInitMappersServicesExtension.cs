using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Command;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Endpoint;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaEmailV1EnableInitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaEmailV1EnableInitCommandResult, MfaEmailV1EnableInitEndpointResponse>,
            MfaEmailV1EnableInitCommandResultToEndpointResponseMapper>();
        return services;
    }
}

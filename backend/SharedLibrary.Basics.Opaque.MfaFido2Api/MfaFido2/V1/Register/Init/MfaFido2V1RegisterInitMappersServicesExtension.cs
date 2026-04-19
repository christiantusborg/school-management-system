using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Command;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Endpoint;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaFido2V1RegisterInitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaFido2V1RegisterInitCommandResult, MfaFido2V1RegisterInitEndpointResponse>,
            MfaFido2V1RegisterInitCommandResultToEndpointResponseMapper>();
        return services;
    }
}

using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Command;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Endpoint;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaFido2V1RegisterFinishMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaFido2V1RegisterFinishCommandResult, MfaFido2V1RegisterFinishEndpointResponse>,
            MfaFido2V1RegisterFinishCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<MfaFido2V1RegisterFinishEndpointRequest, MfaFido2V1RegisterFinishCommand>,
            MfaFido2V1RegisterFinishEndpointRequestToCommandMapper>();
        return services;
    }
}

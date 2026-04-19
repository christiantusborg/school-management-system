using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Command;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Endpoint;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaFido2V1DeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaFido2V1DeleteCommandResult, MfaFido2V1DeleteEndpointResponse>,
            MfaFido2V1DeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

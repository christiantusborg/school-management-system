using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Command;
using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Endpoint;
using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaTotpV1DeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaTotpV1DeleteCommandResult, MfaTotpV1DeleteEndpointResponse>,
            MfaTotpV1DeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Command;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Endpoint;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaEmailV1DeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaEmailV1DeleteCommandResult, MfaEmailV1DeleteEndpointResponse>,
            MfaEmailV1DeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

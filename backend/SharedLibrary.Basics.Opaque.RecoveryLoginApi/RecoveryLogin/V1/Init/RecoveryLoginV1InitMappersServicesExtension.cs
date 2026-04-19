using SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Command;
using SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Endpoint;
using SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class RecoveryLoginV1InitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<RecoveryLoginV1InitCommandResult, RecoveryLoginV1InitEndpointResponse>,
            RecoveryLoginV1InitCommandResultToEndpointResponseMapper>();
        return services;
    }
}

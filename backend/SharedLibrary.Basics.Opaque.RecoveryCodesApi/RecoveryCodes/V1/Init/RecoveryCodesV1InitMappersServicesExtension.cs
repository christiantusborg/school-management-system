using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Command;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Endpoint;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class RecoveryCodesV1InitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<RecoveryCodesV1InitCommandResult, RecoveryCodesV1InitEndpointResponse>,
            RecoveryCodesV1InitCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<RecoveryCodesV1InitEndpointRequest, RecoveryCodesV1InitCommand>,
            RecoveryCodesV1InitEndpointRequestToCommandMapper>();
        return services;
    }
}

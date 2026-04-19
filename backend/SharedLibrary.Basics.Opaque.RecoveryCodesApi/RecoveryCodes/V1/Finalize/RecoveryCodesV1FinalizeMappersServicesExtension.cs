using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Command;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Endpoint;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class RecoveryCodesV1FinalizeMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<RecoveryCodesV1FinalizeCommandResult, RecoveryCodesV1FinalizeEndpointResponse>,
            RecoveryCodesV1FinalizeCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<RecoveryCodesV1FinalizeEndpointRequest, RecoveryCodesV1FinalizeCommand>,
            RecoveryCodesV1FinalizeEndpointRequestToCommandMapper>();
        return services;
    }
}

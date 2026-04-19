using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Command;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Endpoint;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class RecoveryCodesV1GetStatusMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<RecoveryCodesV1GetStatusCommandResult, RecoveryCodesV1GetStatusEndpointResponse>,
            RecoveryCodesV1GetStatusCommandResultToEndpointResponseMapper>();
        return services;
    }
}

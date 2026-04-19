using SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Command;
using SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Endpoint;
using SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class MfaStatusV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaStatusV1GetCommandResult, MfaStatusV1GetEndpointResponse>,
            MfaStatusV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}

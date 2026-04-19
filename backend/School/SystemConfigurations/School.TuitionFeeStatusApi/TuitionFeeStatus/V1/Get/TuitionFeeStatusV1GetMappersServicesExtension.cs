using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Endpoint.Mappers;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class TuitionFeeStatusV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<TuitionFeeStatusV1GetCommandResult, TuitionFeeStatusV1GetEndpointResponse>,
            TuitionFeeStatusV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}

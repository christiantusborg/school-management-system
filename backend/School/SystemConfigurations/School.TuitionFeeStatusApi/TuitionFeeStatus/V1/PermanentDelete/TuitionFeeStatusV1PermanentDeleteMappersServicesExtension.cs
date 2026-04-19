using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Endpoint.Mappers;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class TuitionFeeStatusV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<TuitionFeeStatusV1PermanentDeleteCommandResult, TuitionFeeStatusV1PermanentDeleteEndpointResponse>,
            TuitionFeeStatusV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

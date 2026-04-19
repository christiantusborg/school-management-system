using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Endpoint.Mappers;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class TuitionFeeStatusV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<TuitionFeeStatusV1SoftDeleteCommandResult, TuitionFeeStatusV1SoftDeleteEndpointResponse>,
            TuitionFeeStatusV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

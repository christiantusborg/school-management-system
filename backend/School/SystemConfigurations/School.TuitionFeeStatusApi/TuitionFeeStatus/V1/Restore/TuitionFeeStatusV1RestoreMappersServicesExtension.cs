using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Endpoint.Mappers;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class TuitionFeeStatusV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<TuitionFeeStatusV1RestoreCommandResult, TuitionFeeStatusV1RestoreEndpointResponse>,
            TuitionFeeStatusV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}

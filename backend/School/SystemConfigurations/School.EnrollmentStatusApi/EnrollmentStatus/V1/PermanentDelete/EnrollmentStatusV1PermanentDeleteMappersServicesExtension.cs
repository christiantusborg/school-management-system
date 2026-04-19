using School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Endpoint.Mappers;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EnrollmentStatusV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EnrollmentStatusV1PermanentDeleteCommandResult, EnrollmentStatusV1PermanentDeleteEndpointResponse>,
            EnrollmentStatusV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

using School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Endpoint.Mappers;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EnrollmentStatusV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EnrollmentStatusV1SoftDeleteCommandResult, EnrollmentStatusV1SoftDeleteEndpointResponse>,
            EnrollmentStatusV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

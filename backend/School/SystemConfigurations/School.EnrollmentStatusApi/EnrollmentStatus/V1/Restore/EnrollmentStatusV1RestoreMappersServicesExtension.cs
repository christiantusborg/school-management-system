using School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Endpoint.Mappers;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EnrollmentStatusV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EnrollmentStatusV1RestoreCommandResult, EnrollmentStatusV1RestoreEndpointResponse>,
            EnrollmentStatusV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}

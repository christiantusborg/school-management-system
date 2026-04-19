using School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Endpoint.Mappers;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EnrollmentStatusV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EnrollmentStatusV1GetCommandResult, EnrollmentStatusV1GetEndpointResponse>,
            EnrollmentStatusV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}

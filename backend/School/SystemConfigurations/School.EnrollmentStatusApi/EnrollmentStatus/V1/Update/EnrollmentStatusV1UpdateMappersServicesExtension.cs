using School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint.Mappers;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EnrollmentStatusV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EnrollmentStatusV1UpdateEndpointRequest, EnrollmentStatusV1UpdateCommand>,
            EnrollmentStatusV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<EnrollmentStatusV1UpdateCommandResult, EnrollmentStatusV1UpdateEndpointResponse>,
            EnrollmentStatusV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}

using School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint.Mappers;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EnrollmentStatusV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EnrollmentStatusV1CreateEndpointRequest, EnrollmentStatusV1CreateCommand>,
            EnrollmentStatusV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<EnrollmentStatusV1CreateCommandResult, EnrollmentStatusV1CreateEndpointResponse>,
            EnrollmentStatusV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}

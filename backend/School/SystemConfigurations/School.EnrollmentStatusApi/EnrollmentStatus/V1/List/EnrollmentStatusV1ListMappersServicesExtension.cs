using School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Endpoint.Mappers;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EnrollmentStatusV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<EnrollmentStatusV1ListCommandResultItem>, BaseGetAllResponse<EnrollmentStatusV1ListEndpointResponseItem>>,
            EnrollmentStatusV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}

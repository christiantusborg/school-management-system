using School.SubjectApi.Subject.V1.SoftDelete.Command;
using School.SubjectApi.Subject.V1.SoftDelete.Endpoint;
using School.SubjectApi.Subject.V1.SoftDelete.Endpoint.Mappers;

namespace School.SubjectApi.Subject.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class SubjectV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<SubjectV1SoftDeleteCommandResult, SubjectV1SoftDeleteEndpointResponse>,
            SubjectV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

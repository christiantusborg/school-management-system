using School.SubjectApi.Subject.V1.PermanentDelete.Command;
using School.SubjectApi.Subject.V1.PermanentDelete.Endpoint;
using School.SubjectApi.Subject.V1.PermanentDelete.Endpoint.Mappers;

namespace School.SubjectApi.Subject.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class SubjectV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<SubjectV1PermanentDeleteCommandResult, SubjectV1PermanentDeleteEndpointResponse>,
            SubjectV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

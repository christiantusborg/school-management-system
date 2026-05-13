using School.PathwayApi.EducationLevel.V1.SoftDelete.Command;
using School.PathwayApi.EducationLevel.V1.SoftDelete.Endpoint;
using School.PathwayApi.EducationLevel.V1.SoftDelete.Endpoint.Mappers;

namespace School.PathwayApi.EducationLevel.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EducationLevelV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EducationLevelV1SoftDeleteCommandResult, EducationLevelV1SoftDeleteEndpointResponse>,
            EducationLevelV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

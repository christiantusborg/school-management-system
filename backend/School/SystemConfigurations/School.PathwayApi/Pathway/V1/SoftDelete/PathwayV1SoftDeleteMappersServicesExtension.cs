using School.PathwayApi.Pathway.V1.SoftDelete.Command;
using School.PathwayApi.Pathway.V1.SoftDelete.Endpoint;
using School.PathwayApi.Pathway.V1.SoftDelete.Endpoint.Mappers;

namespace School.PathwayApi.Pathway.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PathwayV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PathwayV1SoftDeleteCommandResult, PathwayV1SoftDeleteEndpointResponse>,
            PathwayV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}

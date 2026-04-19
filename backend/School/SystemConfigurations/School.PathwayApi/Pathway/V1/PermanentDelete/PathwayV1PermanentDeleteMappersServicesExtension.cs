using School.PathwayApi.Pathway.V1.PermanentDelete.Command;
using School.PathwayApi.Pathway.V1.PermanentDelete.Endpoint;
using School.PathwayApi.Pathway.V1.PermanentDelete.Endpoint.Mappers;

namespace School.PathwayApi.Pathway.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PathwayV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PathwayV1PermanentDeleteCommandResult, PathwayV1PermanentDeleteEndpointResponse>,
            PathwayV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
